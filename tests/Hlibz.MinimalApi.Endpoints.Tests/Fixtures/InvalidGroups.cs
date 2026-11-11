namespace Hlibz.MinimalApi.Endpoints.Tests.Fixtures.InvalidGroups;

internal sealed class FirstEndpointGroup : IEndpointGroup
{
    public string Prefix => "first";
}

internal sealed class SecondEndpointGroup : IEndpointGroup
{
    public string Prefix => "second";
}

internal sealed class NeedsPrefixEndpointGroup : IEndpointGroup
{
    public NeedsPrefixEndpointGroup(string prefix)
    {
        Prefix = prefix;
    }

    public string Prefix { get; }
}
