namespace Gateway.HateoasConfigurations;

public class Thought : IBaseConfiguration
{
    public string ApiTitle { get; } = "WebApi";
    public string SchemaName { get; } = nameof(Thought);

    public Dictionary<string, string> Links { get; } = new()
    {
        {  "get-thought-by-id", "self" }
    };
}