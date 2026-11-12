using Envelope.Api.Books;

using Hlibz.MinimalApi.Endpoints;
using Hlibz.Redoc.Extensions;
using Hlibz.Results.AspNetCore;

using Library.Api;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(HeaderAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(
        HeaderAuthenticationHandler.SchemeName,
        configureOptions: null);
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("member", policy => policy.RequireRole("member"));

// The 401/403 the authorization middleware answers before any handler runs are envelopes.
builder.Services.AddResultErrorResponses(options => options.UseEnvelope = true);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<BookStore>();

WebApplication app = builder.Build();

// A request ASP.NET can't bind (malformed JSON, say) throws before the handler runs; answer
// that, and any other failure, with an envelope too.
app.UseExceptionHandler(errorApp => errorApp.Run(context =>
{
    bool badRequest =
        context.Features.Get<IExceptionHandlerFeature>()?.Error is BadHttpRequestException;
    context.Response.StatusCode = badRequest
        ? StatusCodes.Status400BadRequest
        : StatusCodes.Status500InternalServerError;
    return context.Response.WriteErrorAsync(
        badRequest ? "The request is malformed." : "Something went wrong.",
        code: null,
        context.RequestAborted);
}));

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapReDocDocuments([new ReDocDocument("v1", "Envelope API")]);

// Handlers return a Result or Result<T>; WithResultEnvelopes sends it as an envelope and
// documents 200 ResultEnvelope<T>. The errors (ProducesErrors, and the 401/403 of endpoints
// requiring authorization) are documented with the envelope's schema.
app.MapEndpoints(
    typeof(Program).Assembly,
    options => options
        .UseErrorResponse<ResultEnvelope>()
        .ConfigureEndpoints(endpoint => endpoint.Builder.WithResultEnvelopes()));

await app.RunAsync();
