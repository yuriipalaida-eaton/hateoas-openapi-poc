namespace Gateway.HateoasConfigurations;

public class Topic : IConditionalConfiguration
{
    public string ApiTitle { get; } = "WebApi";
    public string SchemaName { get; } = nameof(Topic);

    public Dictionary<string, string> Links { get; } = new()
    {
        { "get-topic-by-title", "self" }
    };

    public Dictionary<string, (string, Type, Func<object, bool>)> Conditions { get; } = new()
    {
        { "self", ("opened", typeof(bool), x => (bool)x) }
    };
}