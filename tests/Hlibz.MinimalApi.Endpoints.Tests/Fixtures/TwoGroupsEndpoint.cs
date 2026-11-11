using Hlibz.MinimalApi.Endpoints.Tests.Fixtures.InvalidGroups;

using Microsoft.AspNetCore.Http;

namespace Hlibz.MinimalApi.Endpoints.Tests.Fixtures.TwoGroups;

internal sealed class TwoGroupsEndpoint
    : IEndpoint<FirstEndpointGroup>, IEndpoint<SecondEndpointGroup>
{
    public HttpMethod Method => HttpMethod.Get;

    public string Route => string.Empty;

    public Delegate Handler => () => Results.Ok();
}
