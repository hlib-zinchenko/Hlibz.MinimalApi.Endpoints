![Hlibz.MinimalApi.Endpoints icon](https://raw.githubusercontent.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints/main/docs/icon.png)

# Hlibz.MinimalApi.Endpoints

[![CI](https://github.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints/actions/workflows/ci.yml/badge.svg)](https://github.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints/blob/main/LICENSE)<br>
[![Hlibz.MinimalApi.Endpoints](https://img.shields.io/nuget/v/Hlibz.MinimalApi.Endpoints?label=Hlibz.MinimalApi.Endpoints)](https://www.nuget.org/packages/Hlibz.MinimalApi.Endpoints)
[![downloads](https://img.shields.io/nuget/dt/Hlibz.MinimalApi.Endpoints?label=downloads)](https://www.nuget.org/packages/Hlibz.MinimalApi.Endpoints)<br>
[![Hlibz.MinimalApi.Endpoints.FluentValidation](https://img.shields.io/nuget/v/Hlibz.MinimalApi.Endpoints.FluentValidation?label=Hlibz.MinimalApi.Endpoints.FluentValidation)](https://www.nuget.org/packages/Hlibz.MinimalApi.Endpoints.FluentValidation)
[![downloads](https://img.shields.io/nuget/dt/Hlibz.MinimalApi.Endpoints.FluentValidation?label=downloads)](https://www.nuget.org/packages/Hlibz.MinimalApi.Endpoints.FluentValidation)

One class per minimal API endpoint, found and mapped for you, with its OpenAPI responses
documented from what the endpoint *is* rather than from what you remembered to write.

| Package | What it gives you |
|---|---|
| `Hlibz.MinimalApi.Endpoints` | `IEndpoint<TGroup, TResponse>`, `IEndpointGroup`, `app.MapEndpoints(assemblies)`, per-endpoint opt-ins, group and endpoint conventions, `EndpointDiscovery`, and the source generator that lists the endpoints at compile time. No dependencies beyond ASP.NET Core. |
| `Hlibz.MinimalApi.Endpoints.FluentValidation` | `options.AddFluentValidation()`: every handler parameter with a registered `IValidator<T>` is validated before the handler runs. |

Targets .NET 8, 9, 10 and 11.

## Why

Minimal APIs scale badly as one long `Program.cs`, and the usual fix, a `MapOrderEndpoints`
extension per feature, still leaves the same five lines on every route: the name, the
response type, the error codes, the 401/403 when it needs a user, the 429 when it's rate
limited. This package keeps the REPR shape (one class per endpoint) while staying plain
minimal APIs: the handler is the same delegate you'd pass to `MapGet`, with the same
parameter binding, filters and `TypedResults`. No base class, no custom pipeline. A source
generator lists the endpoints while your project compiles, but there is nothing to learn about
it: it ships inside the package and needs no setup.

## Quick start

```bash
dotnet add package Hlibz.MinimalApi.Endpoints
```

```csharp
internal sealed class OrdersEndpointGroup : IEndpointGroup
{
    public string Prefix => "api/orders";

    public IReadOnlyList<string> Tags => ["Orders"];
}

/// <summary>Gets one order.</summary>
internal sealed class GetOrderEndpoint : IEndpoint<OrdersEndpointGroup, OrderDto>
{
    public HttpMethod Method => HttpMethod.Get;

    public string Route => "{id:int}";

    public IReadOnlyList<int> ProducesErrors => [StatusCodes.Status404NotFound];

    public string? AuthorizationPolicy => "Member";

    public Delegate Handler => Handle;

    private static async Task<IResult> Handle(
        int id,
        [FromServices] IOrderService orders,
        CancellationToken cancellationToken) =>
        await orders.FindAsync(id, cancellationToken) is { } order
            ? Results.Ok(order)
            : Results.NotFound();
}
```

```csharp
app.MapEndpoints(typeof(Program).Assembly);
```

`GET api/orders/{id}` is now mapped, named `GetOrder`, tagged `Orders`, requires the
`Member` policy, and documents `200` as `OrderDto` plus `404`, `401` and `403` as
`ProblemDetails`.

## The endpoint

| Member | Default | What it does |
|---|---|---|
| `Method` | required | `HttpMethod.Get`, `.Post`, ... |
| `Route` | required | Relative to the group prefix; `string.Empty` for the group root. |
| `Handler` | required | The route handler delegate, bound exactly like `MapGet`'s. |
| `SuccessStatusCode` | `200` | The status `TResponse` and `FileContentType` are documented under (e.g. `201`). |
| `ProducesErrors` | `[]` | Error statuses, documented with the error schema. |
| `AuthorizationPolicy` | `null` | `RequireAuthorization(policy)`. |
| `RateLimitPolicy` | `null` | `RequireRateLimiting(policy)`. |
| `MaxUploadBytes` | `null` | A file upload: the request body limit, antiforgery off, `413` documented. |
| `FileContentType` | `null` | A file download: the success response documented as that content type. |
| `Configure(builder)` | nothing | Anything else on the `RouteHandlerBuilder`. |

`IEndpoint<TGroup>` maps the endpoint into `TGroup`; `IEndpoint<TGroup, TResponse>` also
documents the success response. The route name is the class name minus a trailing
`Endpoint`. Endpoints and groups are created once, at startup, with their parameterless
constructor (public or internal), so they hold no state: dependencies come in as handler
parameters.

## The group

| Member | Default | What it does |
|---|---|---|
| `Prefix` | required | `MapGroup(prefix)`. |
| `Tags` | `[]` | `WithTags(...)` on every endpoint. |
| `AuthorizationPolicy` | `null` | Every endpoint requires it. |
| `RateLimitPolicy` | `null` | Every endpoint is subject to it. |
| `Configure(builder)` | nothing | Anything else on the `RouteGroupBuilder`, e.g. `WithGroupName`. |

A group is found through the endpoints that name it, so it can live in any assembly.

## Documented responses

* `200` (or `SuccessStatusCode`) with `TResponse`, or the file content type.
* Every `ProducesErrors` status, with the error schema.
* `401` and `403` whenever the endpoint **ends up** requiring authorization, whatever set it:
  the endpoint, its group, a convention, or an outer `MapGroup`. `AllowAnonymous()` turns them
  off again.
* `429` whenever the endpoint ends up with a rate limiting policy and no
  `DisableRateLimiting()`.
* `413` on uploads.

The gate responses are decided in a `Finally` convention, on the endpoint's final metadata, so
they can't drift from the real behaviour. A status already documented is never added twice.
Turn them off with `DocumentAuthorizationResponses` / `DocumentRateLimitResponses`.

## Options

```csharp
app.MapEndpoints(assemblies, options =>
{
    options.UseErrorResponse<ErrorResponse>();          // your error body; ProblemDetails by default
    options.EndpointName = type => type.Name;           // or null for unnamed routes
    options.IncludeEndpoint = type => type.Namespace != "Legacy";
});
```

| Option | Default |
|---|---|
| `ErrorResponseType` / `ErrorContentType` | `ProblemDetails`, `application/problem+json`; `null` type documents statuses without a body. |
| `EndpointName` | `EndpointMappingOptions.DefaultEndpointName` (`GetOrderEndpoint` → `GetOrder`). |
| `IncludeEndpoint` | every endpoint type. |
| `DocumentAuthorizationResponses` / `DocumentRateLimitResponses` | `true`. |
| `GroupConventions` / `EndpointConventions` | none; see below. |

## Conventions: your app's own rules

App-wide rules are conventions, run after each group's or endpoint's own members, in the order
added. They can change the builder or throw to fail startup.

```csharp
internal interface IAdminEndpointGroup : IEndpointGroup;

app.MapEndpoints(assemblies, options => options
    // Admin groups, and only admin groups, live under api/admin/.
    .ConfigureGroups(context =>
    {
        bool isAdmin = context.Group is IAdminEndpointGroup;
        if (isAdmin != context.Group.Prefix.StartsWith("api/admin/", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{context.GroupType.Name} is misplaced.");
        }
    })
    // Every admin group is gated as a whole and skips rate limiting.
    .ConfigureGroups<IAdminEndpointGroup>((_, group) => group
        .RequireAuthorization("Admin")
        .DisableRateLimiting())
    // Every endpoint's class summary becomes its OpenAPI description.
    .ConfigureEndpoints(context =>
    {
        if (MyXmlDocs.Summary(context.EndpointType) is { } summary)
        {
            context.Builder.WithDescription(summary);
        }
    }));
```

`EndpointContext` gives the endpoint and its type, the group, the `RouteHandlerBuilder`, the
root services, the options, and `ProducesError(status)` to document an error with the
configured schema. `EndpointGroupContext` gives the same for a group.

## Discovery without mapping

`EndpointDiscovery.Discover(assemblies)` returns the groups and their endpoints exactly as
`MapEndpoints` would map them, for anything that has to agree with the routes, such as
registering one OpenAPI document per kind of group:

```csharp
bool hasAdmin = EndpointDiscovery.Discover(assemblies)
    .Any(group => group.Group is IAdminEndpointGroup);
```

## How endpoints are found

Nothing is scanned at runtime. The package's source generator runs on every project that
references it, directly or through another project or package, and writes the list of that
assembly's endpoints into the assembly: each concrete, non-generic class implementing
`IEndpoint<TGroup>` (directly or through a base class), its group and response type, and a
`new` for each. `MapEndpoints` and `EndpointDiscovery` only read that list, so discovery adds
no reflection scan to startup and is trimming- and Native AOT-safe. Abstract and open generic
classes are bases for endpoints, not endpoints, and are left out.

An assembly passed to `MapEndpoints` that wasn't compiled with the generator fails at startup
with a message saying so.

## Build errors

An endpoint the generator can't list fails the build, pointing at the class:

| Id | Cause |
|---|---|
| `HMAE001` | Implements `IEndpoint` directly, without `IEndpoint<TGroup>`. |
| `HMAE002` | Names more than one group. |
| `HMAE003` | Names more than one response type. |
| `HMAE004` | The endpoint or its group has no public or internal parameterless constructor. |
| `HMAE005` | The group is an interface or an abstract class. |
| `HMAE006` | The endpoint, its group or its response type is private (or nested in a private type). |

## FluentValidation

```bash
dotnet add package Hlibz.MinimalApi.Endpoints.FluentValidation
```

```csharp
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

app.MapEndpoints(assemblies, options => options.AddFluentValidation());
```

For every endpoint, every handler parameter type with an `IValidator<T>` registered gets an
endpoint filter that validates the argument (`[FromBody]`, `[AsParameters]`, anything) before
the handler runs, and the failure is documented. Nothing to opt into per endpoint: register a
validator and it applies. Which types have a validator is decided once at startup, through
`IServiceProviderIsService`; the validator itself is resolved from the request's services, so
scoped validators (`AddValidatorsFromAssembly`'s default) work.

A failure answers with a `400` validation problem by default. Answer with your own:

```csharp
// With Hlibz.Results.AspNetCore + Hlibz.Results.FluentValidation:
options.AddFluentValidation(validation =>
    validation.OnFailure = (result, _) => result.ToErrorResult().ToHttpResult());
```

A custom `OnFailure` is documented with the mapping's error schema; set `FailureStatusCode`
when it isn't `400` (e.g. `422`), or `null` to document nothing. Works with FluentValidation
11.0.0 and later.

## Using it with Hlibz.Results

[Hlibz.Results.AspNetCore](https://www.nuget.org/packages/Hlibz.Results.AspNetCore) writes one
`ErrorResponse` body for every failure, the auth gate's included. Document it as the error
schema and every documented error matches what the API sends:

```csharp
app.MapEndpoints(assemblies, options => options
    .UseErrorResponse<ErrorResponse>()
    .AddFluentValidation(validation =>
        validation.OnFailure = (result, _) => result.ToErrorResult().ToHttpResult()));
```

## Limitations

* Discovery is trimming- and Native AOT-safe, but mapping isn't: each `Handler` is a `Delegate`
  bound through `RequestDelegateFactory`, exactly like a `MapGet` call that ASP.NET Core's
  Request Delegate Generator can't see. `MapEndpoints` carries `RequiresUnreferencedCode` and
  `RequiresDynamicCode` to say so.
* Endpoints are singletons created at startup: no constructor injection.

## Samples

[`samples/Library.Api`](https://github.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints/tree/main/samples/Library.Api): public and admin groups, a group
convention, FluentValidation, authorization, rate limiting, an upload and a CSV download,
with a `.http` file for every case and the OpenAPI document at `/openapi/v1.json`.

```bash
dotnet run --project samples/Library.Api    # http://localhost:5190
```

## License

MIT
