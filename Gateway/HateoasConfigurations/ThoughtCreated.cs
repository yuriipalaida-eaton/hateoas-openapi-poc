namespace Gateway.HateoasConfigurations;

public class ThoughtCreated : IBaseConfiguration
{
    public string ApiTitle { get; } = "WebApi";
    public string SchemaName { get; } = nameof(ThoughtCreated);

    public Dictionary<string, string> Links { get; } = new()
    {
        { "get-thought-by-id", "self" }
    };
}