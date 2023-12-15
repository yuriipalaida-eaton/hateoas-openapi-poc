namespace Gateway.HateoasConfigurations;

/// <summary>
///     An interface to mark all HATEOAS configurations.
///     Since there is nothing about .NET classes, we can modify it and use JSON-files for configuration, for example.
/// </summary>
public interface IBaseConfiguration
{
    // To differentiate between multiple services that the gateway communicates with.
    // Can be simplified with abstract classes per service.
    public string ApiTitle { get; }
    
    // The schema name the configuration is applicable to. 
    // Basically, the name of the class (that should be unique for OpenAPI)
    // Represents keys in the "components.schemas" object in OpenAPI
    public string SchemaName { get; }
    
    // Links to be added to the response
    // The key is the operation id (the name of the controller's endpoint in ASP.NET world)
    // IMPORTANT! With current implementation, it's assumed the link should be unique per gateway (among all the services).
    public Dictionary<string, string> Links { get; }
}

public interface IConditionalConfiguration : IBaseConfiguration
{
    public Dictionary<string, (string, Type, Func<object, bool>)> Conditions { get; }
}
