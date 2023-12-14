namespace Gateway.HateoasConfigurations;

public class ThoughtList : IBaseConfiguration
{
    public string ApiTitle { get; } = "WebApi";
    public string SchemaName { get; } = nameof(ThoughtList);

    public Dictionary<string, string> Links { get; } = new()
    {
        {  "get-thoughts", "self" }
    };
}