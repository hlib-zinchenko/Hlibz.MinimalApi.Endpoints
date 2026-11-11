using System.ComponentModel;

namespace Hlibz.MinimalApi.Endpoints;

/// <summary>
/// The endpoints the package's source generator found in an assembly, which
/// <see cref="EndpointDiscovery"/> reads instead of scanning the assembly. The generator derives
/// one per assembly and applies it to the assembly; don't derive or apply it yourself.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class EndpointManifestAttribute : Attribute
{
    /// <summary>
    /// Gets the assembly's endpoints, in declaration order.
    /// </summary>
    public abstract IReadOnlyList<EndpointRegistration> Endpoints { get; }
}

/// <summary>
/// One endpoint in an <see cref="EndpointManifestAttribute"/>: its type, its group and response
/// type, and how to create it and its group without reflection. Written by the source generator.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class EndpointRegistration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointRegistration"/> class.
    /// </summary>
    /// <param name="endpointType">The endpoint type.</param>
    /// <param name="createEndpoint">Creates the endpoint.</param>
    /// <param name="groupType">The group named in <see cref="IEndpoint{TGroup}"/>.</param>
    /// <param name="createGroup">Creates the group.</param>
    /// <param name="responseType">
    /// The success response named in <see cref="IEndpoint{TGroup, TResponse}"/>, or
    /// <see langword="null"/>.
    /// </param>
    public EndpointRegistration(
        Type endpointType,
        Func<IEndpoint> createEndpoint,
        Type groupType,
        Func<IEndpointGroup> createGroup,
        Type? responseType)
    {
        ArgumentNullException.ThrowIfNull(endpointType);
        ArgumentNullException.ThrowIfNull(createEndpoint);
        ArgumentNullException.ThrowIfNull(groupType);
        ArgumentNullException.ThrowIfNull(createGroup);

        EndpointType = endpointType;
        CreateEndpoint = createEndpoint;
        GroupType = groupType;
        CreateGroup = createGroup;
        ResponseType = responseType;
    }

    /// <summary>
    /// Gets the endpoint type.
    /// </summary>
    public Type EndpointType { get; }

    /// <summary>
    /// Gets the function that creates the endpoint.
    /// </summary>
    public Func<IEndpoint> CreateEndpoint { get; }

    /// <summary>
    /// Gets the group named in <see cref="IEndpoint{TGroup}"/>.
    /// </summary>
    public Type GroupType { get; }

    /// <summary>
    /// Gets the function that creates the group.
    /// </summary>
    public Func<IEndpointGroup> CreateGroup { get; }

    /// <summary>
    /// Gets the success response named in <see cref="IEndpoint{TGroup, TResponse}"/>, or
    /// <see langword="null"/>.
    /// </summary>
    public Type? ResponseType { get; }
}
