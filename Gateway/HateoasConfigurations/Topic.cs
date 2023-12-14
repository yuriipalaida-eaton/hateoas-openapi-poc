namespace Gateway.HateoasConfigurations;

public class Topic : IBaseConfiguration
{
    public string ApiTitle { get; } = "WebApi";
    public string SchemaName { get; } = nameof(Topic);

    public Dictionary<string, string> Links { get; } = new()
    {
        { "get-topic-by-title", "self" }
    };
}