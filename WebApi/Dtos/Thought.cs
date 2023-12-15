namespace WebApi.Dtos;

public class Thought
{
    public Guid ThoughtId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string Description { get; set; }
    public Topic Topic { get; set; }
    public List<string> Places { get; set; }
    public bool PlacesAvailable => Places?.Count >= 1;
}