namespace Gateway.HateoasConfigurations;

public class ConfigurationsResolver
{
    // Surely, can be simplified
    private readonly Dictionary<string, Type> _configurations = new()
    {
        { nameof(ThoughtCreated).ToLower(), typeof(ThoughtCreated) },
        { nameof(ThoughtList).ToLower(), typeof(ThoughtList) },
        { nameof(Thought).ToLower(), typeof(Thought) },
        { nameof(Topic).ToLower(), typeof(Topic) }
    };

    public IBaseConfiguration ResolveConfiguration(string schemaName) =>
        (IBaseConfiguration)Activator.CreateInstance(_configurations[schemaName.ToLower()]);
}