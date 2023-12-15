using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Gateway.HateoasConfigurations;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Ocelot.Middleware;

namespace Gateway;

public class HateoasMiddleware(ConfigurationsResolver configurationsResolver) : IMiddleware
{
    // Can be cached, pre-downloaded in advance, etc.
    private OpenApiDocument _openApiDocument;
    
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Getting the OpenAPI document from the service with all the links (paths) and schemas of responses.
        // We take those links, replace placeholders with real values and add them to the responses.
        // We use schemas of responses to know what object to add specific links to.
        _openApiDocument ??= await GetOpenApiDocument();

        var response = context.Response;
        var originalBody = response.Body;
        using var newBody = new MemoryStream();
        response.Body = newBody;

        await next.Invoke(context);

        // A response returned from the service. Needs to be serialized and iterated to add links to all the arrays and objects based on configurations.
        var downstreamResponse = context.Items.DownstreamResponse();

        // We need schemas for all the nested objects/arrays. While iterating we'll be able to find them by references,
        // however for the root element we infer it based on the path.
        var responseSchema = GetResponseSchema(context);

        // The overriden response with links for all the objects/arrays (incl. nested ones)

        // The whole root JSON
        var content = await downstreamResponse.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(content);
        var resultWithLinks = TraverseAndAddLinks(JsonNode.Parse(jsonDocument.RootElement.ToString()), responseSchema)
            .ToJsonString();

        // Probably, can be simplified a little bit. Copy-paste from SO.
        // Just overriding the response with the modified one.
        var responseBodyStream = response.Body;
        responseBodyStream.SetLength(0);
        await using var writer = new StreamWriter(responseBodyStream, leaveOpen: true);
        await writer.WriteAsync(resultWithLinks);
        await writer.FlushAsync();
        response.ContentLength = responseBodyStream.Length;

        newBody.Seek(0, SeekOrigin.Begin);
        await newBody.CopyToAsync(originalBody);
        response.Body = originalBody;
    }
    
    private JsonNode TraverseAndAddLinks(JsonNode node, OpenApiSchema schema)
    {
        switch (node)
        {
            // If the property is an array (which MUST NOT be a root element), we get the schema (type) of its elements from the parent
            case JsonArray jsonArray:
                var newArray = new JsonArray();
                foreach (var item in jsonArray)
                {
                    newArray.Add(TraverseAndAddLinks(item, schema).DeepClone());
                }
                return new JsonObject
                {
                    [node.GetPropertyName()] = newArray
                };
            // If the property is an object, we look for its configuration by the name of the schema) and then do another switch by each of its properties,
            // either of which can be another object or array.
            case JsonObject jsonObject:
                Dictionary<string, string> linksWithPlaceholders = null;
                if (schema != null)
                {
                    var configuration = GetHateoasConfiguration(schema.Reference.Id);
                    // Initially, add all the links from the configuration
                    linksWithPlaceholders = GenerateLinks(configuration);
                    HandleConditions(configuration, jsonObject, linksWithPlaceholders);
                }

                var newObject = new JsonObject();
                foreach (var kvp in jsonObject)
                {
                    if (kvp.Value == null)
                    {
                        continue;
                    }
                    switch (kvp.Value)
                    {
                        case JsonObject obj:
                            // We find this property's schema by its name in the OpenAPI
                            var objectElementSchema = schema?.Properties.FirstOrDefault(x => x.Key == kvp.Key).Value;
                            newObject.Add(kvp.Key, TraverseAndAddLinks(obj, objectElementSchema).DeepClone());
                            break;
                        case JsonArray arr:
                            // For arrays, we find the schema by the "Items" properties and pass it to each item in the array
                            var arrayElementSchema =
                                schema?.Properties.FirstOrDefault(x => x.Key == kvp.Key).Value.Items;
                            newObject.Add("_embedded", TraverseAndAddLinks(arr, arrayElementSchema).DeepClone());
                            break;
                        default:
                            newObject.Add(kvp.Key, kvp.Value?.DeepClone());
                            break;
                    }
                }

                if (linksWithPlaceholders == null || linksWithPlaceholders.Count == 0)
                {
                    return newObject;
                }
                
                var links = ReplacePlaceholders(linksWithPlaceholders, jsonObject);

                newObject.Add("_links", JsonNode.Parse(JsonSerializer.Serialize(links)));

                return newObject;
            // If the node is a primitive value we return it as it is, otherwise we have different behavior for objects and arrays
            default:
                return node;
        }
    }

    private static void HandleConditions(IBaseConfiguration configuration, JsonObject jsonObject,
        Dictionary<string, string> linksWithPlaceholders)
    {
        if (configuration is not IConditionalConfiguration conditionalConfiguration)
        {
            return;
        }
        
        // Iterate over all the conditions and if some of them are met, remove the corresponding links
        foreach (var (linkName, (propertyName, type, condition)) in conditionalConfiguration.Conditions)
        {
            if (jsonObject[propertyName] == null)
            {
                continue;
            }

            object convertedValue;
            try
            {
                convertedValue = typeof(JsonNode)
                    .GetMethod("GetValue", Array.Empty<Type>())
                    ?.MakeGenericMethod(type)
                    .Invoke(jsonObject[propertyName], null);
            }
            // Can happen only if developer uses a non-primitive type for condition (or an array)
            catch
            {
                continue;
            }
                            
            if (!condition(convertedValue))
            {
                linksWithPlaceholders.Remove(linkName);
            }
        }
    }

    private static Dictionary<string, string> ReplacePlaceholders(Dictionary<string, string> linksWithPlaceholders, JsonObject jsonObject)
    {
        var links = new Dictionary<string, string>();
        foreach (var (name, link) in linksWithPlaceholders)
        {
            var regex = new Regex(@"\{.*?\}");
            var matches = regex.Matches(link);

            var added = false;
            foreach (Match match in matches)
            {
                var key = match.Value.Trim('{', '}');
                if (jsonObject.ContainsKey(key))
                {
                    links.Add(name, link.Replace(match.Value, jsonObject[key].ToString()));
                    added = true;
                }
            }

            if (!added)
            {
                links.Add(name, link);
            }
        }

        return links;
    }
    
    private Dictionary<string, string> GenerateLinks(IBaseConfiguration configuration)
    {
        // Take the name of the link from the HATEOAS configuration and find the corresponding path in the OpenAPI.
        return configuration.Links.ToDictionary(
            x => x.Value,
            x => _openApiDocument.Paths.SingleOrDefault(p => p.Value.Operations.Any(o =>
                o.Value.OperationId == x.Key)).Key);
    }

    private static async Task<OpenApiDocument> GetOpenApiDocument()
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7051/")
        };

        var stream = await httpClient.GetStreamAsync("swagger/v1/swagger.json");

        return new OpenApiStreamReader().Read(stream, out var diagnostic);
    }

    private OpenApiSchema GetResponseSchema(HttpContext context)
    {
        // Finding a path in the OpenAPI which corresponds to the downstream route template.
        var pathItem = _openApiDocument.Paths.FirstOrDefault(path =>
            path.Key == context.Items.DownstreamRouteHolder().Route.DownstreamRoute.FirstOrDefault()
                ?.DownstreamPathTemplate.Value);

        var schema = pathItem.Value
            // Important, because the same path can return different schemas depending on the operation type
            .Operations[GetOperationType(context.Request.Method)]
            .Responses["200"]
            .Content["application/json"].Schema;

        return schema;
    }

    private static OperationType GetOperationType(string method)
    {
        return method switch
        {
            "GET" => OperationType.Get,
            "POST" => OperationType.Post,
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };
    }

    private IBaseConfiguration GetHateoasConfiguration(string schemaName)
    {
        return configurationsResolver.ResolveConfiguration(schemaName);
    }
}