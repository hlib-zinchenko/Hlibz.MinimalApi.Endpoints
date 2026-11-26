using Hlibz.MinimalApi.Endpoints;
using Hlibz.Results.AspNetCore;

using Library.Api;

using Microsoft.AspNetCore.Authentication;

using TypedErrors.Api.Books;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(HeaderAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(
        HeaderAuthenticationHandler.SchemeName,
        configureOptions: null);
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("member", policy => policy.RequireRole("member"));

// One error body (ErrorResponse) for every failure: the handlers' declared errors with their
// data in details, and the authorization middleware's 401/403 too.
builder.Services.AddResultErrorResponses();

builder.Services.AddOpenApi();
builder.Services.AddSingleton<BookStore>();

WebApplication app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();

// Every handler returns a Result<T, TError>. WithResultResponses() sends it and documents each
// declared error's status from the error set; UseErrorResponse documents the rest (401/403 of
// the endpoints requiring authorization, and ProducesErrors) with the same body.
app.MapEndpoints(
    typeof(Program).Assembly,
    options => options
        .UseErrorResponse<ErrorResponse>()
        .ConfigureEndpoints(endpoint => endpoint.Builder.WithResultResponses()));

await app.RunAsync();
