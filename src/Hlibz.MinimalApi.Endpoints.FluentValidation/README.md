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

ASP.NET Core 10's `AddValidation()` (DataAnnotations) is a separate filter; with both on,
both run, so validate a type with one or the other.

A failure is a `400` validation problem by default; answer with your own instead:

```csharp
options.AddFluentValidation(validation =>
{
    validation.OnFailure = (result, httpContext) => result.ToErrorResult().ToHttpResult();
    validation.FailureStatusCode = StatusCodes.Status400BadRequest; // what's documented
});
```

Works with FluentValidation 11.0.0 and later.

Full documentation and a sample: [https://github.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints](https://github.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints).
