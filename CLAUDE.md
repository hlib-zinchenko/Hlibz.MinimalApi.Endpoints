# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this is

Two NuGet packages released together from one tag:

- `src/Hlibz.MinimalApi.Endpoints`: `IEndpoint` (two members: `Map(IEndpointRouteBuilder group)`
  holding the endpoint's one `MapGet`/`MapPost`/... chain, and `string? Name`) /
  `IEndpoint<TGroup>`, `IEndpointGroup`, `EndpointDiscovery`
  (reads the generated `EndpointManifestAttribute` of each assembly →
  `EndpointGroupDescriptor`/`EndpointDescriptor`; no reflection scan), `MapEndpoints`
  (`EndpointRouteBuilderExtensions`), `EndpointResponseExtensions` (`ProducesErrors`,
  `ProducesFile`, `AcceptsUpload` on `RouteHandlerBuilder`), `ProducesErrorMetadata`,
  `EndpointMappingOptions` (error schema, naming, include filter, group/endpoint conventions),
  `EndpointContext`/`EndpointGroupContext`. Only the ASP.NET Core shared framework.
  The success response comes from ASP.NET's inferred metadata (the handler's return type);
  declared errors (`ProducesErrorMetadata`) and 401/403/429 are
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
  operation tree and reports `HMAE007` (not exactly one route: one call taking the group as its
  first argument and returning a `RouteHandlerBuilder`, so an existing one-route extension such
  as Hlibz.Results.AspNetCore's `MapErrorCodes()` is allowed; it trusts such a helper). There is no response type to
  check: the handler's return type is the contract, whatever result library it comes from
  (`HMAE003`/`HMAE008`/`HMAE009` are retired, don't reuse them). All but `HMAE011` are build errors
  (`Diagnostics.cs`, `AnalyzerReleases.Unshipped.md`, the README's "Build errors").
  The library packs it under `analyzers/dotnet/cs` plus `buildTransitive/*.targets`, which adds
  it wherever it's missing (a guard: the .NET 10 and 11 SDKs already pass it on to indirect
  consumers, despite pack's `exclude="Analyzers"`; checked on 10.0.201 and 11.0.100).
  `.github/scripts/check-packed-generator.sh` (CI, after Pack) builds and runs an indirect
  consumer of the packed nupkgs. Tests,
  samples and anything else in this repo reference it as `OutputItemType="Analyzer"`; its own
  tests (`tests/...Generators.Tests`, net11.0 only) compile sources in memory and load the result
  (`GeneratorRun`), or run the analyzer on them (`AnalyzerRun`).
  `IsAotCompatible` is on for the library: discovery is AOT-safe, `MapEndpoints` is annotated
  `RequiresUnreferencedCode`/`RequiresDynamicCode` because handlers bound by the `Map*` call in
  each endpoint's `Map` may go through `RequestDelegateFactory` (not yet proven otherwise).
- `src/Hlibz.MinimalApi.Endpoints.FluentValidation`: `options.AddFluentValidation()`, an endpoint
  convention adding an `AddEndpointFilterFactory` that, when routing first builds the endpoint,
  finds the handler's values with a registered `IValidator<T>` (`ValidationTarget.For`: each
  parameter by position, plus the properties of `[AsParameters]` parameters; checked with
  `IServiceProviderIsService`, resolved from `RequestServices` per request) and attaches one
  `ValidationEndpointFilter`. A `Builder.Add` callback makes the same decision to document the
  failure, since a filter factory can't add metadata. Failure via
  `FluentValidationEndpointOptions.OnFailure`.

Born from Lighthouse's `Common.API/Endpoints`; its admin-audience rules are conventions here
(see the README and `samples/Library.Api/Program.cs`).

- `samples/Library.Api`: net11.0 only, never packed, references the library projects. Keep its
  `.http` file and `samples/README.md` in step with the responses.
- `samples/Envelope.Api` (same rules, :5191): handlers return Hlibz.Results' `Result`/`Result<T>`;
  Hlibz.Results' `WithResultEnvelopes()` (applied in `ConfigureEndpoints`) sends and documents them
  as envelopes, `UseErrorResponse<ResultEnvelope>()` documents the errors.

## Requires

* The .NET SDK `global.json` pins (11.0.100, patches roll forward; Dependabot bumps it), plus
  the .NET 10 runtime to run both test targets. Pinned because warnings are errors and
  `AnalysisMode=All`: a new SDK feature band brings new analyzer rules.

## Commands

```bash
dotnet build
dotnet test
dotnet test -f net11.0
dotnet pack -c Release -o ./nupkg
dotnet run --project samples/Library.Api    # :5190, try Library.Api.http
dotnet run --project samples/Envelope.Api   # :5191, try Envelope.Api.http
```

## Conventions

StyleCop + Sonar + `AnalysisMode=All`, warnings as errors; every public member has XML docs
(CS1591 on for `src`). Explicit types, no `var`. Tests: xUnit v3 on Microsoft Testing Platform,
named `Subject_WithCondition_ExpectedOutcome`; each scenario's endpoints live in their own
`Fixtures` namespace and tests map only that namespace (`IncludeEndpoint`), so invalid fixtures
don't break the others. Every fixture must pass the analyzer; invalid `Map` shapes are tested
only as in-memory
sources in `Generators.Tests`. Versions are central
(`Directory.Packages.props`); the shared NuGet metadata and `Version` live in
`Directory.Build.props`, release by pushing a `vX.Y.Z` tag (see Releasing).
Packing runs package validation (`EnablePackageValidation` in `src/Directory.Build.props`), so
the API must match across target frameworks; add `PackageValidationBaselineVersion` once 1.0.0
ships.

Release policy: both packages always ship together under one version (lockstep).
FluentValidation stays at the oldest version the add-on supports (11.0.0; Dependabot ignores it).

Each package has its own `src/<Package>/README.md` (packed by `src/Directory.Build.props`, shown
on its nuget.org page): short, with absolute links. The root `README.md` is the full
documentation; keep them in step when an API changes.

## Releasing

1. Set `Version` in `Directory.Build.props`, commit, then push the tag `vX.Y.Z`.
2. The user approves the `release` environment run.
3. Once the run succeeds and
   `https://api.nuget.org/v3-flatcontainer/<lowercase id>/index.json` lists the version, Claude
   creates the GitHub release that `PackageReleaseNotes` links to:
   `gh release create vX.Y.Z --verify-tag --title X.Y.Z [--prerelease when the version has a suffix] --notes-file <notes>`.
   Claude writes the notes from `git log <previous tag>..vX.Y.Z`: breaking changes first, then
   features and fixes, then one nuget.org link per package. Nothing is attached.
