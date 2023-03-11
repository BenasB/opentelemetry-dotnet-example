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
        .ConfigureResource(builder => builder.AddService(serviceName))
        .AddAspNetCoreInstrumentation();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/hello", () =>
{
    return Results.Ok();
});

app.Run();