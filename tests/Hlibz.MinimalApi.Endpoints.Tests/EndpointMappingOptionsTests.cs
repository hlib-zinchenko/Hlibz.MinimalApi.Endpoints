namespace Hlibz.MinimalApi.Endpoints.Tests;

public sealed class EndpointMappingOptionsTests
{
    [Theory]
    [InlineData(typeof(SampleEndpoint), "Sample")]
    [InlineData(typeof(Endpoint), "Endpoint")]
    [InlineData(typeof(SampleHandler), "SampleHandler")]
    public void DefaultEndpointName_WithType_StripsTrailingEndpoint(Type type, string expected)
    {
        Assert.Equal(expected, EndpointMappingOptions.DefaultEndpointName(type));
    }

    private sealed class SampleEndpoint;

    private sealed class Endpoint;

    private sealed class SampleHandler;
}
