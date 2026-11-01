using UserManagementAPI.Configuration;
using UserManagementAPI.Endpoints;
using UserManagementAPI.Middleware;
using UserManagementAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<UserStore>();
builder.Services.AddSingleton<RequestAuditLogger>();
builder.Services.AddOptions<ApiAuthenticationOptions>()
    .Bind(builder.Configuration.GetSection(ApiAuthenticationOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Token),
        "ApiAuthentication:Token must contain a non-empty bearer token.")
    .ValidateOnStart();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<ApiTokenAuthenticationMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapGet("/api/testing/unhandled-error", TriggerUnhandledError)
        .ExcludeFromDescription();
}

app.MapGet("/", () => Results.Ok(new
{
    name = "User Management API",
    usersEndpoint = "/api/users"
}))
.WithName("GetApiInformation")
.WithTags("API");

app.MapUserEndpoints();

app.Run();

static IResult TriggerUnhandledError() =>
    throw new InvalidOperationException("Simulated unhandled exception for middleware verification.");
