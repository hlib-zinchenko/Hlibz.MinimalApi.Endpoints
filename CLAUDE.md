# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this is

Two NuGet packages released together from one tag:

- `src/Hlibz.MinimalApi.Endpoints`: `IEndpoint` (two members: `Map(IEndpointRouteBuilder group)`
  holding the endpoint's one `MapGet`/`MapPost`/... chain, and `string? Name`) /
  `IEndpoint<TGroup>` / `IEndpoint<TGroup, TResponse>`, `IEndpointGroup`, `EndpointDiscovery`
  (reads the generated `EndpointManifestAttribute` of each assembly →
  `EndpointGroupDescriptor`/`EndpointDescriptor`; no reflection scan), `MapEndpoints`
  (`EndpointRouteBuilderExtensions`), `EndpointResponseExtensions` (`ProducesErrors`,
  `ProducesFile`, `AcceptsUpload` on `RouteHandlerBuilder`), `ProducesErrorMetadata`,
  `EndpointMappingOptions` (error schema, naming, include filter, group/endpoint conventions),
  `EndpointContext`/`EndpointGroupContext`. Only the ASP.NET Core shared framework.
  The success response comes from ASP.NET's inferred metadata (the handler's return type);
  declared errors (`ProducesErrorMetadata`), the `200 TResponse` fallback and 401/403/429 are
  resolved by one `Finally` convention on the endpoint's final metadata
  (`Internal/ResponseDocumentation`), so they follow whatever set authorization or rate
  limiting; keep it that way rather than inspecting the endpoint. The operation description
  is the endpoint class's XML `<summary>`, which the generator reads from the comment trivia
  (`SummaryReader`, so it works without `GenerateDocumentationFile`) into the manifest
  (`EndpointRegistration.Description` → `EndpointDescriptor.Description`); a `Finally` in
  `MapEndpoint` adds it only when no other description is set.
- `src/Hlibz.MinimalApi.Endpoints.Generators`: the incremental source generator and, beside it,
  `EndpointMapAnalyzer` (netstandard2.0, Roslyn 4.8, never packed on its own). The generator
  writes one `EndpointManifest_<AssemblyName>` per compilation that references the library and
  reports unusable endpoints as `HMAE001`-`HMAE006`, struct endpoints as `HMAE010` and
  generic ones (never mapped; abstract bases are skipped) as the warning `HMAE011`; the
  analyzer reads each `Map` body's
  operation tree and reports `HMAE007` (not exactly one route), `HMAE008`/`HMAE009` (handler
  doesn't return / can't be checked against `TResponse`). All but `HMAE011` are build errors
  (`Diagnostics.cs`, `AnalyzerReleases.Unshipped.md`, the README's "Build errors").
  The library packs it under `analyzers/dotnet/cs` plus `buildTransitive/*.targets`, which adds
  it to projects that get the package indirectly (NuGet withholds analyzers there). Tests,
  samples and anything else in this repo reference it as `OutputItemType="Analyzer"`; its own
  tests (`tests/...Generators.Tests`, net11.0 only) compile sources in memory and load the result
  (`GeneratorRun`), or run the analyzer on them (`AnalyzerRun`).
  `IsAotCompatible` is on for the library: discovery is AOT-safe, `MapEndpoints` is annotated
  `RequiresUnreferencedCode`/`RequiresDynamicCode` because handlers bound by the `Map*` call in
  each endpoint's `Map` may go through `RequestDelegateFactory` (not yet proven otherwise).
- `src/Hlibz.MinimalApi.Endpoints.FluentValidation`: `options.AddFluentValidation()`, an endpoint
  convention that, in a `Builder.Add` callback (when routing first builds the endpoint), reads
  the handler's `MethodInfo` and attaches `ValidationEndpointFilter<T>` per parameter type with a
  registered `IValidator<T>` (checked with `IServiceProviderIsService`, resolved from
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
don't break the others. Every fixture must pass the analyzer: fixtures with `TResponse` return
typed results (`Ok<T>`, `Results<...>`), and one that needs an opaque `IResult` handler wraps
`Map` in `#pragma warning disable HMAE009`; invalid `Map` shapes are tested only as in-memory
sources in `Generators.Tests`. Versions are central
(`Directory.Packages.props`); the shared NuGet metadata and `Version` live in
`Directory.Build.props`, release by pushing a `vX.Y.Z` tag.

Release policy: both packages always ship together under one version (lockstep).
FluentValidation stays at the oldest version the add-on supports (11.0.0; Dependabot ignores it).

Each package has its own `src/<Package>/README.md` (packed by `src/Directory.Build.props`, shown
on its nuget.org page): short, with absolute links. The root `README.md` is the full
documentation; keep them in step when an API changes.
