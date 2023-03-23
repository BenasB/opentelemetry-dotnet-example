using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Roomr.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var serviceName = typeof(Program).Assembly.GetName().Name ?? "Unknown service";
builder.Services.AddOpenTelemetry().WithTracing(builder =>
{
    builder
        .AddConsoleExporter()
        .AddOtlpExporter(options =>
        {
            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        })
        .ConfigureResource(builder => builder.AddService(serviceName))
        .AddAspNetCoreInstrumentation();
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/hello", async (IConfiguration configuration) =>
{
    Baggage.Current.SetBaggage("hello", "world");
    var authUrl = configuration.GetValue<string>("AuthUrl");
    var client = new HttpClient()
    {
        BaseAddress = new Uri(authUrl)
    };

    var newUser = new User
    {
        Username = Guid.NewGuid().ToString(),
        Password = Guid.NewGuid().ToString()
    };

    await client.PostAsJsonAsync($"register", newUser);
    var response = await client.PostAsJsonAsync($"login", newUser);
    var user = await response.Content.ReadFromJsonAsync<User>();

    if (user is null)
        return Results.Problem("Failed to register or log in");

    Activity.Current?.SetTag("auth.username", user.Username);

    return Results.Ok(user);
});

app.Run();