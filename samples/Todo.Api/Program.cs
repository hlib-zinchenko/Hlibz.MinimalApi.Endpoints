// Todo: where three packages meet. Hlibz.MinimalApi.Endpoints maps one class per endpoint,
// Hlibz.Results turns the Result each handler returns into its response (and picks the
// success status code), and Hlibz.OpenApi.AspNetCore finishes the OpenAPI document both of
// them describe. Run it and open http://localhost:5192/api-docs; samples/README.md says what
// to look for where.

using Hlibz.MinimalApi.Endpoints;
using Hlibz.OpenApi;
using Hlibz.Redoc.Extensions;
using Hlibz.Results.AspNetCore;

using Todo.Api.Lists;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Hlibz.Results: the success status code, app-wide. A DELETE that succeeds is 204 No Content;
// every other method keeps the default 200 (a POST included). Endpoints that create something
// say so themselves with WithCreatedLocation: 201 Created plus a Location header.
builder.Services.AddResultErrorResponses(options =>
    options.SuccessStatusCodes[HttpMethods.Delete] = StatusCodes.Status204NoContent);

// Hlibz.OpenApi.AspNetCore: IDs documented as the integers they wrap, the rules written on the
// requests, every description from an XML <summary> (Hlibz.Results' ErrorResponse's included),
// and "Gets the list's id." read as "The list's id.". A lambda, so Microsoft.AspNetCore.OpenApi's
// XML comment generator intercepts the call too.
builder.Services.AddOpenApi(options => options
    .AddXmlDocumentation()
    .AddStronglyTypedIds()
    .AddOpenApiRules()
    .AddDescriptionCleanup());

builder.Services.AddSingleton<TodoStore>();

WebApplication app = builder.Build();

app.MapOpenApi();

// The ReDoc page at /api-docs, with Hlibz.Redoc's Try it console: send any request from the page
// and see the status, headers and body come back, matched against what the operation documents.
app.MapReDocDocuments([new ReDocDocument("v1", "Todo API")], docs => docs.EnableTryIt());

// Hlibz.MinimalApi.Endpoints: every endpoint class in the assembly. Handlers return a Result or
// Result<T>; Hlibz.Results' WithResultResponses sends it as ToHttpResult() would (the value, or
// an ErrorResponse) and documents the success with the status code it's sent with.
// ProducesErrors documents the failures with the same ErrorResponse schema.
app.MapEndpoints(
    typeof(Program).Assembly,
    options => options
        .UseErrorResponse<ErrorResponse>()
        .ConfigureEndpoints(endpoint => endpoint.Builder.WithResultResponses()));

await app.RunAsync();
