using Microsoft.AspNetCore.Mvc;
using WebApi.Dtos;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

var topic = new Topic { Title = "Misc" };
var topics = new List<Topic> { topic };
var thoughts = new List<Thought>
{
    new()  { ThoughtId = Guid.NewGuid(), OccurredAt = DateTimeOffset.Now, Description = "Create new HATEOAS impl,"},
    new() { ThoughtId = Guid.NewGuid(), OccurredAt = DateTimeOffset.Now, Description = "Do not mess it up", Topic = topic }
};
var thoughtList = new ThoughtList { Thoughts = thoughts, Total = thoughts.Count };

app.MapGet("/thoughts", () => thoughtList).WithName("get-thoughts");
app.MapGet("/thoughts/{thoughtId:guid}", (Guid thoughtId) => thoughts.FirstOrDefault(x => x.ThoughtId == thoughtId)).WithName("get-thought-by-id");
app.MapPost("thoughts", ([FromBody] NewThought body) =>
{
    var thought = new Thought()
        { ThoughtId = Guid.NewGuid(), OccurredAt = DateTimeOffset.Now, Description = body.Description };
    return new ThoughtCreated() { Id = thought.ThoughtId };
}).WithName("create-thought");
app.MapDelete("thoughts/{thoughtId:guid}", (Guid thoughtId) => thoughts.RemoveAll(x => x.ThoughtId == thoughtId)).WithName("delete-thought");
app.MapGet("topics/{title}", (string title) => topics.FirstOrDefault(x => x.Title == title)).WithName("get-topic-by-title");
app.UseSwagger();
app.Run();