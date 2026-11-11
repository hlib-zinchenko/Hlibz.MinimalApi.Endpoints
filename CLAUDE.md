# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this is

Two NuGet packages released together from one tag:

- `src/Hlibz.MinimalApi.Endpoints`: `IEndpoint` / `IEndpoint<TGroup>` /
  `IEndpoint<TGroup, TResponse>`, `IEndpointGroup`, `EndpointDiscovery` (reflection scan →
  `EndpointGroupDescriptor`/`EndpointDescriptor`), `MapEndpoints` (`EndpointRouteBuilderExtensions`),
  `EndpointMappingOptions` (error schema, naming, include filter, group/endpoint conventions),
  `EndpointContext`/`EndpointGroupContext`. Only the ASP.NET Core shared framework.
  The 401/403/429 responses are documented by a `Finally` convention on the endpoint's final
  metadata (`Internal/ResponseDocumentation`), so they follow whatever set authorization or rate
  limiting; keep it that way rather than checking the interface members.
- `src/Hlibz.MinimalApi.Endpoints.FluentValidation`: `options.AddFluentValidation()`, an endpoint
  convention that attaches `ValidationEndpointFilter<T>` per handler parameter type with a
  registered `IValidator<T>` (checked with `IServiceProviderIsService` at startup, resolved from
  `RequestServices` per request). Failure via `FluentValidationEndpointOptions.OnFailure`.

Born from Lighthouse's `Common.API/Endpoints`; its admin-audience rules are conventions here
(see the README and `samples/Library.Api/Program.cs`).

- `samples/Library.Api`: net11.0 only, never packed, references the library projects. Keep its
  `.http` file and `samples/README.md` in step with the responses.

## Requires

* .NET 11 SDK, plus the .NET 8, 9 and 10 runtimes to run every test target.

## Commands

```bash
dotnet build
dotnet test
dotnet test -f net11.0
dotnet pack -c Release -o ./nupkg
dotnet run --project samples/Library.Api    # :5190, try Library.Api.http
```

## Conventions

StyleCop + Sonar + `AnalysisMode=All`, warnings as errors; every public member has XML docs
(CS1591 on for `src`). Explicit types, no `var`. Tests: xUnit v3 on Microsoft Testing Platform,
named `Subject_WithCondition_ExpectedOutcome`; each scenario's endpoints live in their own
`Fixtures` namespace and tests map only that namespace (`IncludeEndpoint`), so invalid fixtures
don't break the others. Fixture handlers return `IResult`, not `TypedResults` types, so ASP.NET's
inferred response metadata doesn't mix with what the package documents. Versions are central
(`Directory.Packages.props`); the shared NuGet metadata and `Version` live in
`Directory.Build.props`, release by pushing a `vX.Y.Z` tag.

Release policy: both packages always ship together under one version (lockstep).
FluentValidation stays at the oldest version the add-on supports (11.0.0; Dependabot ignores it).

Each package has its own `src/<Package>/README.md` (packed by `src/Directory.Build.props`, shown
on its nuget.org page): short, with absolute links. The root `README.md` is the full
documentation; keep them in step when an API changes.
