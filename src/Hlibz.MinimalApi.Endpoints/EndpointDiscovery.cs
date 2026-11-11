namespace Hlibz.MinimalApi.Endpoints;

/// <summary>
/// Finds the endpoints in a set of assemblies and the groups they belong to, without mapping
/// anything. <c>MapEndpoints</c> maps exactly what this returns, so the app can use it for
/// anything that must agree with the mapped routes, such as one OpenAPI document per group kind.
/// </summary>
/// <remarks>
/// Nothing is scanned at runtime: the package's source generator lists every endpoint of an
/// assembly when it compiles (an <see cref="EndpointManifestAttribute"/>), and reports an endpoint
/// it can't use as a build error. Discovery only reads that list, so it is trimming-safe.
/// </remarks>
public static class EndpointDiscovery
{
    /// <summary>
    /// Finds every concrete <see cref="IEndpoint"/> in <paramref name="assemblies"/> and groups
    /// it under the <see cref="IEndpointGroup"/> it names. A group is found through its endpoints,
    /// so it may live in any assembly, and a group with no endpoint is left out.
    /// </summary>
    /// <param name="assemblies">The assemblies to read.</param>
    /// <param name="includeEndpoint">Which endpoint types to keep; all of them when omitted.</param>
    /// <returns>The groups, in the order their first endpoint was found.</returns>
    /// <exception cref="InvalidOperationException">
    /// An assembly wasn't compiled with the package's source generator, so it has no endpoint
    /// list to read.
    /// </exception>
    public static IReadOnlyList<EndpointGroupDescriptor> Discover(
        IEnumerable<Assembly> assemblies,
        Func<Type, bool>? includeEndpoint = null)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        List<EndpointRegistration> registrations =
        [
            .. assemblies
                .Distinct()
                .SelectMany(Manifest)
                .Where(registration => includeEndpoint?.Invoke(registration.EndpointType) ?? true),
        ];

        Dictionary<Type, IEndpointGroup> groups = [];
        List<EndpointDescriptor> endpoints = new(registrations.Count);
        foreach (EndpointRegistration registration in registrations)
        {
            if (!groups.ContainsKey(registration.GroupType))
            {
                groups[registration.GroupType] = registration.CreateGroup();
            }

            endpoints.Add(new EndpointDescriptor(
                registration.EndpointType,
                registration.CreateEndpoint(),
                registration.GroupType,
                registration.ResponseType));
        }

        return
        [
            .. groups.Select(pair => new EndpointGroupDescriptor(
                pair.Key,
                pair.Value,
                [.. endpoints.Where(endpoint => endpoint.GroupType == pair.Key)])),
        ];
    }

    private static IReadOnlyList<EndpointRegistration> Manifest(Assembly assembly) =>
        assembly.GetCustomAttribute<EndpointManifestAttribute>()?.Endpoints
        ?? throw new InvalidOperationException(
            $"{assembly.GetName().Name} has no endpoint list: the Hlibz.MinimalApi.Endpoints "
            + "source generator didn't run when it was compiled. Reference the "
            + "Hlibz.MinimalApi.Endpoints package from that project, and don't exclude its "
            + "analyzers.");
}
