namespace Hlibz.MinimalApi.Endpoints;

/// <summary>
/// Finds the endpoints in a set of assemblies and the groups they belong to, without mapping
/// anything. <c>MapEndpoints</c> maps exactly what this returns, so the app can use it for
/// anything that must agree with the mapped routes, such as one OpenAPI document per group kind.
/// </summary>
public static class EndpointDiscovery
{
    /// <summary>
    /// Finds every concrete <see cref="IEndpoint"/> in <paramref name="assemblies"/> and groups
    /// it under the <see cref="IEndpointGroup"/> it names. A group is found through its endpoints,
    /// so it may live in any assembly, and a group with no endpoint is left out.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <param name="includeEndpoint">Which endpoint types to keep; all of them when omitted.</param>
    /// <returns>The groups, in the order their first endpoint was found.</returns>
    /// <exception cref="InvalidOperationException">
    /// An endpoint or group can't be used: it implements <see cref="IEndpoint"/> without a group,
    /// names more than one group or response type, or has no parameterless constructor.
    /// </exception>
    public static IReadOnlyList<EndpointGroupDescriptor> Discover(
        IEnumerable<Assembly> assemblies,
        Func<Type, bool>? includeEndpoint = null)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        List<EndpointDescriptor> endpoints =
        [
            .. assemblies
                .Distinct()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type is { IsClass: true, IsAbstract: false }
                               && !type.ContainsGenericParameters
                               && typeof(IEndpoint).IsAssignableFrom(type))
                .Where(type => includeEndpoint?.Invoke(type) ?? true)
                .Select(Describe),
        ];

        Dictionary<Type, IEndpointGroup> groups = [];
        foreach (Type groupType in endpoints
                     .Select(endpoint => endpoint.GroupType)
                     .Where(groupType => !groups.ContainsKey(groupType)))
        {
            groups[groupType] = Create<IEndpointGroup>(groupType);
        }

        return
        [
            .. groups.Select(pair => new EndpointGroupDescriptor(
                pair.Key,
                pair.Value,
                [.. endpoints.Where(endpoint => endpoint.GroupType == pair.Key)])),
        ];
    }

    private static EndpointDescriptor Describe(Type endpointType)
    {
        Type[] interfaces = endpointType.GetInterfaces();

        Type[] groupTypes = GenericArguments(interfaces, typeof(IEndpoint<>), 0);
        Type groupType = groupTypes switch
        {
            [Type single] => single,
            [] => throw new InvalidOperationException(
                $"{endpointType.FullName} implements IEndpoint directly; implement "
                + "IEndpoint<TGroup> or IEndpoint<TGroup, TResponse> instead."),
            _ => throw new InvalidOperationException(
                $"{endpointType.FullName} names more than one endpoint group: "
                + string.Join(", ", groupTypes.Select(type => type.Name)) + "."),
        };

        Type[] responseTypes = GenericArguments(interfaces, typeof(IEndpoint<,>), 1);
        Type? responseType = responseTypes switch
        {
            [] => null,
            [Type single] => single,
            _ => throw new InvalidOperationException(
                $"{endpointType.FullName} names more than one response type: "
                + string.Join(", ", responseTypes.Select(type => type.Name)) + "."),
        };

        return new EndpointDescriptor(
            endpointType,
            Create<IEndpoint>(endpointType),
            groupType,
            responseType);
    }

    private static Type[] GenericArguments(Type[] interfaces, Type definition, int position) =>
    [
        .. interfaces
            .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == definition)
            .Select(type => type.GetGenericArguments()[position])
            .Distinct(),
    ];

    private static T Create<T>(Type type)
    {
        if (type.IsAbstract || type.IsInterface)
        {
            throw new InvalidOperationException(
                $"{type.FullName} is abstract; endpoints and endpoint groups must be concrete "
                + "classes.");
        }

        try
        {
            return (T)Activator.CreateInstance(type, nonPublic: true)!;
        }
        catch (MissingMethodException exception)
        {
            throw new InvalidOperationException(
                $"{type.FullName} has no parameterless constructor; endpoints and endpoint "
                + "groups are created once at startup, so their dependencies belong in the "
                + "handler's parameters.",
                exception);
        }
    }
}
