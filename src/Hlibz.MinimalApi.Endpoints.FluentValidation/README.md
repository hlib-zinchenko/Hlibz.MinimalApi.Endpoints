![Hlibz.MinimalApi.Endpoints.FluentValidation icon](https://raw.githubusercontent.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints/main/docs/icon.png)

# Hlibz.MinimalApi.Endpoints.FluentValidation

[![NuGet](https://img.shields.io/nuget/v/Hlibz.MinimalApi.Endpoints.FluentValidation.svg)](https://www.nuget.org/packages/Hlibz.MinimalApi.Endpoints.FluentValidation)
[![CI](https://github.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints/actions/workflows/ci.yml/badge.svg)](https://github.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints/actions/workflows/ci.yml)

Automatic FluentValidation for
[Hlibz.MinimalApi.Endpoints](https://www.nuget.org/packages/Hlibz.MinimalApi.Endpoints).

```bash
dotnet add package Hlibz.MinimalApi.Endpoints.FluentValidation
```

```csharp
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

app.MapEndpoints(typeof(Program).Assembly, options => options.AddFluentValidation());
```

Every handler parameter type with an `IValidator<T>` registered, and every such property type
of an `[AsParameters]` parameter, gets an endpoint filter that validates the value before the
handler runs, and the failure status is documented. Nothing
to opt into per endpoint. Which types have a validator is decided when routing first builds
each endpoint; validators resolve from the request's services, so any lifetime works.

ASP.NET Core's `AddValidation()` (DataAnnotations) is a separate filter; with both on,
both run, so validate a type with one or the other.

A failure is a `400` validation problem by default; answer with your own instead:

```csharp
options.AddFluentValidation(validation =>
{
    validation.OnFailure = (result, httpContext) => Result.Failure(result.ToValidationFailed()).ToHttpResult();
    validation.FailureStatusCode = StatusCodes.Status400BadRequest; // what's documented
});
```

Works with FluentValidation 11.0.0 and later.

Targets .NET 11. .NET 10 apps stay on 1.x.

## A family of packages

One of five packages I build to set up new .NET APIs the way I want them: Hlibz.Results,
Hlibz.UseCases, Hlibz.MinimalApi.Endpoints, Hlibz.OpenApi and Hlibz.Paging. Each works on its own,
and they work best together, as in [Hlibz.Stack](https://github.com/hlib-zinchenko/Hlibz.Stack). New
minor and major versions come often, a new major whenever .NET gives me a better way to do
something. Breaking changes only ever come in a major and are listed first in its
[release notes](https://github.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints/releases). You're
welcome to use the packages, and to
[open an issue](https://github.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints/issues) when something
is wrong or missing.

Full documentation and samples: [github.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints](https://github.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints).
