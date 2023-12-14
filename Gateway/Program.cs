using Gateway;
using Gateway.HateoasConfigurations;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("ocelot.json");
builder.Services.AddOcelot();
builder.Services.AddTransient<HateoasMiddleware>();
builder.Services.AddSingleton<ConfigurationsResolver>();

// Adding all the registered HATEOAS configurations
builder.Services.Scan(scan => scan
    .FromEntryAssembly()
    .AddClasses(c => c.AssignableTo<IBaseConfiguration>())
    .AsImplementedInterfaces()
    .WithSingletonLifetime());

var app = builder.Build();

app.UseMiddleware<HateoasMiddleware>();
app.UseOcelot().Wait();

app.Run();