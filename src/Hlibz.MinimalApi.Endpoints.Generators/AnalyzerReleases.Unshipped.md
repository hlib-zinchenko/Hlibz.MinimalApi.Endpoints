; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
HMAE001 | Hlibz.MinimalApi.Endpoints | Error | Endpoint implements IEndpoint without a group
HMAE002 | Hlibz.MinimalApi.Endpoints | Error | Endpoint names more than one group
HMAE003 | Hlibz.MinimalApi.Endpoints | Error | Endpoint names more than one response type
HMAE004 | Hlibz.MinimalApi.Endpoints | Error | Endpoint or group has no usable parameterless constructor
HMAE005 | Hlibz.MinimalApi.Endpoints | Error | Endpoint group is abstract
HMAE006 | Hlibz.MinimalApi.Endpoints | Error | Type isn't accessible to the generated code
HMAE007 | Hlibz.MinimalApi.Endpoints | Error | Map doesn't map exactly one route
HMAE008 | Hlibz.MinimalApi.Endpoints | Error | Handler doesn't return the endpoint's response type
HMAE009 | Hlibz.MinimalApi.Endpoints | Error | Handler's response can't be checked against the endpoint's response type
