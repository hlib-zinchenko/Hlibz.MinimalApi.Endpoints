using Microsoft.AspNetCore.Http;

namespace Hlibz.MinimalApi.Endpoints.Tests.Fixtures.Direct;

internal sealed class DirectEndpoint : IEndpoint
{
    public HttpMethod Method => HttpMethod.Get;

    public string Route => string.Empty;

    public Delegate Handler => () => Results.Ok();
}
