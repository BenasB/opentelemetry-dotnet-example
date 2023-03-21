using Microsoft.EntityFrameworkCore;
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
        .AddEntityFrameworkCoreInstrumentation();
});

var usersDatabaseConnectionString = builder.Configuration.GetConnectionString("UsersDatabase");
Console.WriteLine($"Trying to use database connection string: '{usersDatabaseConnectionString}'");
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

app.MapGet("/login", () =>
{
    return Results.Ok("token");
});

app.MapPost("/register", async (string username, string password, AuthDbContext dbContext) =>
{
    var newUser = new User
    {
        Username = username,
        Password = password
    };

    var existingUser = await dbContext.Users.FindAsync(newUser.Username);

    if (existingUser is not null)
        return Results.BadRequest();

    var a = await dbContext.Users.AddAsync(newUser);
    await dbContext.SaveChangesAsync();

    return Results.Created("/", newUser);
});

app.Run();