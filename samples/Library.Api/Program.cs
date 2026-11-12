using System.Threading.RateLimiting;

using FluentValidation;

using Hlibz.MinimalApi.Endpoints;
using Hlibz.MinimalApi.Endpoints.FluentValidation;
using Hlibz.Redoc.Extensions;

using Library.Api;
using Library.Api.Admin;
using Library.Api.Books;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(HeaderAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(
        HeaderAuthenticationHandler.SchemeName,
        configureOptions: null);
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.Member, policy => policy.RequireRole("member", "admin"))
    .AddPolicy(Policies.Admin, policy => policy.RequireRole("admin"));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(Policies.Search, limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<BookStore>();
builder.Services.AddScoped<IValidator<AddBookRequest>, AddBookRequestValidator>();
builder.Services.AddScoped<IValidator<ListBooksQuery>, ListBooksQueryValidator>();

WebApplication app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapOpenApi();

// The OpenAPI document above, rendered as a ReDoc page at /api-docs.
app.MapReDocDocuments([new ReDocDocument("v1", "Library API")]);

// Two app-wide rules as group conventions: admin groups, and only admin groups, live under
// api/admin/; and every admin group requires the admin policy and skips rate limiting. Then
// every request type with a registered validator is validated before its handler runs.
app.MapEndpoints(
    typeof(Program).Assembly,
    options => options
        .ConfigureGroups(context =>
        {
            bool isAdmin = context.Group is IAdminEndpointGroup;
            bool hasAdminPrefix =
                context.Group.Prefix.StartsWith("api/admin/", StringComparison.Ordinal);
            if (isAdmin != hasAdminPrefix)
            {
                throw new InvalidOperationException(
                    $"{context.GroupType.Name}: admin groups, and only admin groups, "
                    + "must be prefixed with 'api/admin/'.");
            }
        })
        .ConfigureGroups<IAdminEndpointGroup>((_, group) => group
            .RequireAuthorization(Policies.Admin)
            .DisableRateLimiting())
        .AddFluentValidation());

await app.RunAsync();
