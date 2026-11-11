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
