using Hlibz.MinimalApi.Endpoints.Tests.Fixtures.InvalidGroups;

using Microsoft.AspNetCore.Http;

namespace Hlibz.MinimalApi.Endpoints.Tests.Fixtures.GroupWithoutConstructor;

internal sealed class ListThingsEndpoint : IEndpoint<NeedsPrefixEndpointGroup>
{
    public HttpMethod Method => HttpMethod.Get;

    public string Route => string.Empty;

    public Delegate Handler => () => Results.Ok();
}
