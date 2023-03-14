using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>{
        {"AuthUrl", "http://localhost:5232"}
    });
}

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/hello", async (IConfiguration configuration) =>
{
    var client = new HttpClient();
    var token = await client.GetAsync($"{configuration.GetValue<string>("AuthUrl")}/login");
    return Results.Ok(token);
});

app.Run();