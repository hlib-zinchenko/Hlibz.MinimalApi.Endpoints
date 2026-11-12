![Hlibz.MinimalApi.Endpoints icon](https://raw.githubusercontent.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints/main/docs/icon.png)

# Hlibz.MinimalApi.Endpoints

[![CI](https://github.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints/actions/workflows/ci.yml/badge.svg)](https://github.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints/blob/main/LICENSE)<br>
[![Hlibz.MinimalApi.Endpoints](https://img.shields.io/nuget/v/Hlibz.MinimalApi.Endpoints?label=Hlibz.MinimalApi.Endpoints)](https://www.nuget.org/packages/Hlibz.MinimalApi.Endpoints)
[![downloads](https://img.shields.io/nuget/dt/Hlibz.MinimalApi.Endpoints?label=downloads)](https://www.nuget.org/packages/Hlibz.MinimalApi.Endpoints)<br>
[![Hlibz.MinimalApi.Endpoints.FluentValidation](https://img.shields.io/nuget/v/Hlibz.MinimalApi.Endpoints.FluentValidation?label=Hlibz.MinimalApi.Endpoints.FluentValidation)](https://www.nuget.org/packages/Hlibz.MinimalApi.Endpoints.FluentValidation)
[![downloads](https://img.shields.io/nuget/dt/Hlibz.MinimalApi.Endpoints.FluentValidation?label=downloads)](https://www.nuget.org/packages/Hlibz.MinimalApi.Endpoints.FluentValidation)

One class per minimal API endpoint, found and mapped for you, with its OpenAPI responses
documented from what the endpoint *is* and checked by the build rather than left to what you
remembered to write.

| Package | What it gives you |
|---|---|
| `Hlibz.MinimalApi.Endpoints` | `IEndpoint<TGroup, TResponse>`, `IEndpointGroup`, `app.MapEndpoints(assemblies)`, `ProducesErrors`/`ProducesFile`/`AcceptsUpload`, group and endpoint conventions, `EndpointDiscovery`, and the source generator and analyzer that list and check the endpoints at compile time. No dependencies beyond ASP.NET Core. |
| `Hlibz.MinimalApi.Endpoints.FluentValidation` | `options.AddFluentValidation()`: every handler parameter with a registered `IValidator<T>` is validated before the handler runs. |

Targets .NET 8, 9, 10 and 11.

## Why

Minimal APIs scale badly as one long `Program.cs`, and the usual fix, a `MapOrderEndpoints`
extension per feature, still leaves the same five lines on every route: the name, the
response type, the error codes, the 401/403 when it needs a user, the 429 when it's rate
limited. This package keeps the REPR shape (one class per endpoint) while staying plain
minimal APIs: each endpoint's `Map` is the `MapGet`/`MapPost` call you'd write anyway, with
the same parameter binding, filters, `TypedResults` and `RouteHandlerBuilder` chain. No custom
pipeline. What stays with the package is what every route repeats: the group, the name, the
error schema and the gate responses. A source generator lists the endpoints and an analyzer
checks each `Map` while your project compiles, but there is nothing to learn about them: they
ship inside the package and need no setup.

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
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("{id:int}", Handle)
            .RequireAuthorization("Member")
            .ProducesErrors(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<OrderDto>, NotFound>> Handle(
        int id,
        [FromServices] IOrderService orders,
        CancellationToken cancellationToken) =>
        await orders.FindAsync(id, cancellationToken) is { } order
            ? TypedResults.Ok(order)
            : TypedResults.NotFound();
}
```

```csharp
app.MapEndpoints(typeof(Program).Assembly);
```

`GET api/orders/{id}` is now mapped, named `GetOrder`, tagged `Orders`, requires the
`Member` policy, and documents `200` as `OrderDto` plus `404`, `401` and `403` as
`ProblemDetails`. The build checks that `Map` maps one route and that the handler returns
`OrderDto`.

## The endpoint

| Member | Default | What it does |
|---|---|---|
| `Map(group)` | required | Maps the endpoint's one route on its group: a `MapGet`/`MapPost`/`MapPut`/`MapDelete`/`MapPatch`/`MapMethods` call, relative to the group prefix (`""` for the group root), plus anything else chained on the builder. Returns that builder. |
| `Name` | `null` | The route name (and operation id); `null` uses the `EndpointName` option. |

`IEndpoint<TGroup>` maps the endpoint into `TGroup`; `IEndpoint<TGroup, TResponse>` also
declares what the handler returns on success, which the build checks (`HMAE008`, `HMAE009`).

Inside `Map`, chain anything a `RouteHandlerBuilder` takes (`RequireAuthorization`,
`RequireRateLimiting`, `WithSummary`, `AddEndpointFilter`, ...) and the package's own:

| Extension | What it does |
|---|---|
| `ProducesErrors(params int[])` | Documents error statuses with the mapping's error schema. |
| `ProducesFile(contentType, status = 200)` | Documents the success response as a file of that content type. |
| `AcceptsUpload(maxBytes)` | A file upload: the request body limit, and the same multipart form limit (instead of the form's own 128 MB), antiforgery off (for token-authenticated APIs; configure it yourself when cookies authenticate the endpoint), `413` documented. |

The route name is the class name minus a trailing `Endpoint`, unless the endpoint sets `Name`;
either wins over a `WithName` inside `Map`. Two endpoints with the same name (say
`GetBookEndpoint` in a books group and an admin group) fail `MapEndpoints`, which names both
types; give one its own `Name`:

```csharp
internal sealed class GetBookEndpoint : IEndpoint<AdminBooksEndpointGroup, BookDto>
{
    public string? Name => "GetAdminBook";

    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("{id:int}", (int id) => TypedResults.Ok(...));
}
```

Endpoints and groups are created once, at startup, with their parameterless constructor
(public or internal), so they hold no state: dependencies come in as handler parameters.

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

* The success response comes from the handler's return type, as ASP.NET Core infers it:
  `Ok<OrderDto>` documents `200 OrderDto`, `Created<OrderDto>` `201 OrderDto`, `NoContent`
  `204`, a plain `OrderDto` `200 OrderDto`. When a `TResponse` endpoint's handler documents
  nothing (bare `IResult` with `HMAE009` suppressed), `200 TResponse` is documented instead.
  `ProducesFile` documents a file download.
* Every `ProducesErrors` status, with the error schema. It replaces a body-less response
  ASP.NET inferred for the same status (the `NotFound` arm of `Results<Ok<T>, NotFound>`), and
  leaves a response the handler documents with a body of its own (`Conflict<T>`).
* `401` and `403` whenever the endpoint **ends up** requiring authorization, whatever set it:
  the endpoint, its group, a convention, or an outer `MapGroup`. `AllowAnonymous()` turns them
  off again.
* `429` whenever the endpoint ends up with a rate limiting policy and no
  `DisableRateLimiting()`.
* `413` on `AcceptsUpload`.

The errors and gate responses are decided in a `Finally` convention, on the endpoint's final metadata, so
they can't drift from the real behaviour. A status already documented is never added twice.
Turn them off with `DocumentAuthorizationResponses` / `DocumentRateLimitResponses`.

### Description from the XML summary

An endpoint class's `<summary>` becomes its OpenAPI operation description:

```csharp
/// <summary>
/// Gets one order by its id; a missing order is a <c>404</c>.
/// </summary>
/// <remarks>Notes for whoever maintains it stay here, out of the API docs.</remarks>
internal sealed class GetOrderEndpoint : IEndpoint<OrdersEndpointGroup, OrderDto>
```

The source generator reads the comment from the source when the endpoint compiles, so nothing
is read at runtime and the project doesn't need `GenerateDocumentationFile`. The text is
CommonMark: `<c>`, `<paramref>`, `<see langword>` and `<see cref>` become code spans,
`<see href>` a link, `<para>` a paragraph, `<code>` a code block, `<list>` items a bullet list.
`<remarks>` and the other tags are left out, and so is `<inheritdoc/>`, which has no text of its
own. A description set anywhere else wins: `WithDescription` in `Map`, on the group or in a
convention, or `[EndpointDescription]` on the handler. Turn it off with
`options.DescribeEndpointsFromSummaries = false`.

The summary is now read by your API's consumers, so write it for them.

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
| `EndpointName` | `EndpointMappingOptions.DefaultEndpointName` (`GetOrderEndpoint` → `GetOrder`); endpoints that set `Name` keep it. |
| `IncludeEndpoint` | every endpoint type. |
| `DocumentAuthorizationResponses` / `DocumentRateLimitResponses` | `true`. |
| `DescribeEndpointsFromSummaries` | `true`: each endpoint's XML `<summary>` is its operation description. |
| `GroupConventions` / `EndpointConventions` | none; see below. |

## Conventions: your app's own rules

App-wide rules are conventions, run after each group's own members or each endpoint's `Map`, in
the order added. They can change the builder or throw to fail startup.

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
configured schema. Call it while the convention runs; inside a `Builder.Add(...)` callback,
which runs later, add a `ProducesErrorMetadata` to the endpoint builder's metadata instead.
`EndpointGroupContext` gives the same for a group.

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
no reflection scan to startup and is trimming- and Native AOT-safe. Abstract classes are bases
for endpoints, not endpoints, and are left out. A struct endpoint is a build error (`HMAE010`),
and a generic one, which can't be created, a warning (`HMAE011`): make it `abstract` if it is
a base.

An assembly passed to `MapEndpoints` that wasn't compiled with the generator fails at startup
with a message saying so.

## Build errors

An endpoint the generator can't list fails the build, pointing at the class, and so does a
`Map` the analyzer rejects, pointing at `Map` or its handler. Only `HMAE011` is a warning (an
error too when your project treats warnings as errors):

| Id | Cause |
|---|---|
| `HMAE001` | Implements `IEndpoint` directly, without `IEndpoint<TGroup>`. |
| `HMAE002` | Names more than one group. |
| `HMAE003` | Names more than one response type. |
| `HMAE004` | The endpoint or its group has no public or internal parameterless constructor. |
| `HMAE005` | The group is an interface or an abstract class. |
| `HMAE006` | The endpoint, its group or its response type is private (or nested in a private type). |
| `HMAE007` | `Map` doesn't map exactly one route: it must call `MapGet`/`MapPost`/`MapPut`/`MapDelete`/`MapPatch`/`MapMethods` once on its group parameter, outside loops and lambdas, and use the parameter nowhere else (no `MapGroup`, no helper taking it). |
| `HMAE008` | The handler of an `IEndpoint<TGroup, TResponse>` returns something that doesn't carry `TResponse`: another type, `void`, `Task`, `NoContent`, `Ok<Other>`. |
| `HMAE009` | The handler of an `IEndpoint<TGroup, TResponse>` can't be checked: it returns `IResult`, `object` or `dynamic` (or a `Task` of one), or isn't a method group or lambda. Return a typed result such as `Ok<T>` or `Results<Ok<T>, NotFound>`, or implement `IEndpoint<TGroup>` instead. |
| `HMAE010` | The endpoint is a struct (a `record struct` too); endpoints are classes. |
| `HMAE011` | **Warning.** The endpoint is generic, or nested in a generic type, so it is never mapped. Make it `abstract` if it is a base for endpoints, and derive non-generic endpoints from it. |

A handler carries `TResponse` when it returns `TResponse`, a typed result of it (`Ok<T>`,
`Created<T>`, ...), or a `Results<...>` with such an arm, optionally in a `Task<>` or
`ValueTask<>`; nullability is ignored. A handler ending in a helper that returns `IResult`
(such as Hlibz.Results' `result.ToHttpResult()`) fails `HMAE009`: suppress it with
`#pragma warning disable HMAE009` around `Map`, and `200 TResponse` is still documented.

What the analyzer can't see:

* Any arm carrying `TResponse` satisfies it, an error arm too: `Results<NotFound<OrderDto>,
  BadRequest>` passes.
* A `Map` inherited from a base class in another assembly isn't analysed.

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
the handler runs, and the failure is documented. The properties of an `[AsParameters]` type
count too: a `[FromBody] AddOrderRequest Body` inside one is validated with
`IValidator<AddOrderRequest>`, whether or not the wrapper has a validator of its own. Nothing to opt into per endpoint: register a
validator and it applies. Which types have a validator is decided once per endpoint, through
`IServiceProviderIsService`, when routing first builds the endpoint (the first request, or
the first read of the OpenAPI document), not during `MapEndpoints`; the validator itself is
resolved from the request's services, so scoped validators (`AddValidatorsFromAssembly`'s
default) work.

ASP.NET Core 10's own validation (`builder.Services.AddValidation()`) is a separate filter for
DataAnnotations attributes and knows nothing of FluentValidation. Turn both on and both run on
the same arguments, each answering a failure its own way, so validate a type with one or the
other.

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

* Discovery is trimming- and Native AOT-safe, but mapping isn't proven to be: each handler is
  bound by the `MapGet`/`MapPost` call inside the endpoint's own `Map`, through
  `RequestDelegateFactory` unless ASP.NET Core's Request Delegate Generator intercepts that
  call. Until that is verified, `MapEndpoints` carries `RequiresUnreferencedCode` and
  `RequiresDynamicCode`.
* Endpoints are singletons created at startup: no constructor injection.

## Samples

[`samples/Library.Api`](https://github.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints/tree/main/samples/Library.Api): public and admin groups, a group
convention, FluentValidation, authorization, rate limiting, an upload and a CSV download,
with a `.http` file for every case and the OpenAPI document at `/openapi/v1.json`, rendered by
ReDoc at `/api-docs`.

```bash
dotnet run --project samples/Library.Api    # http://localhost:5190
```

## License

MIT
