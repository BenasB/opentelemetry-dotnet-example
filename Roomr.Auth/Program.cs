using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation(options =>
            options.EnrichWithIDbCommand = (activity, command) =>
            {
                var databaseName = command.Connection?.Database ?? "Unknown db";
                activity.DisplayName = databaseName;
                activity.SetTag("db.name", databaseName);
            }
        );
});

var usersDatabaseConnectionString = builder.Configuration.GetConnectionString("UsersDatabase");
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseMySql(usersDatabaseConnectionString, ServerVersion.AutoDetect(usersDatabaseConnectionString))
);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AuthDbContext>();
        await context.Database.EnsureCreatedAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the DB.");
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/login", async ([FromBody] User user, AuthDbContext dbContext) =>
{
    var existingUser = await dbContext.Users.FindAsync(user.Username);

    if (existingUser is null)
        return Results.NotFound("Specified user does not exist");

    if (existingUser.Password != user.Password)
        return Results.BadRequest("Password does not match");

    return Results.Ok(existingUser);
});

app.MapPost("/register", async ([FromBody] User newUser, AuthDbContext dbContext) =>
{
    var existingUser = await dbContext.Users.FindAsync(newUser.Username);

    if (existingUser is not null)
        return Results.BadRequest("Specified user already exists");

    var a = await dbContext.Users.AddAsync(newUser);
    await dbContext.SaveChangesAsync();

    return Results.Created("/", newUser);
});

app.Run();