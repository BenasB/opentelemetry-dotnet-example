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

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/hello", async () =>
{
    var client = new HttpClient();
    var token = await client.GetAsync("http://localhost:5232/login");
    return Results.Ok();
});

app.Run();