namespace Gateway.HateoasConfigurations;

public class Thought : IConditionalConfiguration
{
    public string ApiTitle { get; } = "WebApi";
    public string SchemaName { get; } = nameof(Thought);

    /// <summary>
    ///     Key: operationId from OpenAPI
    ///     Value: custom name
    /// </summary>
    public Dictionary<string, string> Links { get; } = new()
    {
        { "get-thought-by-id", "self" },
        { "get-thought-places-by-id", "self:places" }
    };

    /// <summary>
    ///     Key: our custom name (should match the name from the links)
    ///     Tuple:
    ///         - The name of the property used for checking condition
    ///         - The type of this property
    ///         - The actual condition
    /// </summary>
    public Dictionary<string, (string, Type, Func<object, bool>)> Conditions { get; } = new()
    {
        { "self", ("description", typeof(string), x => (string)x != "Confidential") },
        { "self:places", ("placesAvailable", typeof(bool), x => (bool)x) }
    };
}