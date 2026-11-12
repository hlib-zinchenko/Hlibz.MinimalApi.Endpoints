using Microsoft.AspNetCore.Mvc;

namespace Hlibz.MinimalApi.Endpoints;

/// <summary>
/// How <c>MapEndpoints</c> maps and documents endpoints: the error schema, endpoint names, which
/// endpoint types to map, and the app's own conventions for groups and endpoints.
/// </summary>
public sealed class EndpointMappingOptions
{
    private const string EndpointSuffix = "Endpoint";

    /// <summary>
    /// Gets or sets the body type every documented error response carries (the endpoint's
    /// <see cref="EndpointResponseExtensions.ProducesErrors"/>, the <c>413</c> of
    /// <see cref="EndpointResponseExtensions.AcceptsUpload"/>, and the automatic
    /// <c>401</c>/<c>403</c>/<c>429</c>), or <see langword="null"/> to document error status
    /// codes without a body.
    /// Defaults to <see cref="ProblemDetails"/>; <see cref="UseErrorResponse{TError}"/> sets your
    /// own.
    /// </summary>
    public Type? ErrorResponseType { get; set; } = typeof(ProblemDetails);

    /// <summary>
    /// Gets or sets the content type of the documented error responses. Defaults to
    /// <c>application/problem+json</c>.
    /// </summary>
    public string ErrorContentType { get; set; } = "application/problem+json";

    /// <summary>
    /// Gets or sets how an endpoint type is turned into its route name (<c>WithName</c>, which
    /// is also its OpenAPI operation id), or returns <see langword="null"/> for no name. Used for
    /// every endpoint whose <see cref="IEndpoint.Name"/> is <see langword="null"/>. Defaults to
    /// <see cref="DefaultEndpointName"/>.
    /// </summary>
    public Func<Type, string?> EndpointName { get; set; } = DefaultEndpointName;

    /// <summary>
    /// Gets or sets which endpoint types are mapped. Defaults to all of them.
    /// </summary>
    public Func<Type, bool> IncludeEndpoint { get; set; } = static _ => true;

    /// <summary>
    /// Gets or sets a value indicating whether <c>401</c> and <c>403</c> are documented on every
    /// endpoint that ends up requiring authorization, from whichever source: the endpoint, its
    /// group or a convention. Defaults to <see langword="true"/>.
    /// </summary>
    public bool DocumentAuthorizationResponses { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether <c>429</c> is documented on every endpoint that
    /// ends up with a rate limiting policy (and not <c>DisableRateLimiting</c>), from whichever
    /// source. Defaults to <see langword="true"/>.
    /// </summary>
    public bool DocumentRateLimitResponses { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether each endpoint type's XML <c>&lt;summary&gt;</c>
    /// becomes its OpenAPI operation description, unless something else (the endpoint's
    /// <see cref="IEndpoint.Map"/>, its group, a convention or an attribute on the handler) set
    /// one. The source generator reads the summary from the source when the endpoint compiles,
    /// so no documentation file is needed at runtime. Defaults to <see langword="true"/>.
    /// </summary>
    public bool DescribeEndpointsFromSummaries { get; set; } = true;

    /// <summary>
    /// Gets the conventions applied to every route group, in order, after the group's own
    /// <see cref="IEndpointGroup.Configure"/>. A convention may throw to reject a
    /// group at startup.
    /// </summary>
    public IList<Action<EndpointGroupContext>> GroupConventions { get; } =
        new List<Action<EndpointGroupContext>>();

    /// <summary>
    /// Gets the conventions applied to every endpoint, in order, after the endpoint's own
    /// <see cref="IEndpoint.Map"/> and its name. A convention may throw to reject an endpoint at
    /// startup.
    /// </summary>
    public IList<Action<EndpointContext>> EndpointConventions { get; } =
        new List<Action<EndpointContext>>();

    /// <summary>
    /// Gets the endpoint type's name without a trailing <c>Endpoint</c>:
    /// <c>GetOrderEndpoint</c> is named <c>GetOrder</c>.
    /// </summary>
    /// <param name="endpointType">The endpoint type.</param>
    /// <returns>The route name.</returns>
    public static string DefaultEndpointName(Type endpointType)
    {
        ArgumentNullException.ThrowIfNull(endpointType);

        string name = endpointType.Name;
        return name.Length > EndpointSuffix.Length
               && name.EndsWith(EndpointSuffix, StringComparison.Ordinal)
            ? name[..^EndpointSuffix.Length]
            : name;
    }

    /// <summary>
    /// Documents every error response with <typeparamref name="TError"/>.
    /// </summary>
    /// <param name="contentType">The error responses' content type.</param>
    /// <typeparam name="TError">The error body type.</typeparam>
    /// <returns>The options, for chaining.</returns>
    public EndpointMappingOptions UseErrorResponse<TError>(
        string contentType = "application/json")
    {
        ArgumentNullException.ThrowIfNull(contentType);

        ErrorResponseType = typeof(TError);
        ErrorContentType = contentType;
        return this;
    }

    /// <summary>
    /// Adds a convention applied to every route group.
    /// </summary>
    /// <param name="convention">The convention.</param>
    /// <returns>The options, for chaining.</returns>
    public EndpointMappingOptions ConfigureGroups(Action<EndpointGroupContext> convention)
    {
        ArgumentNullException.ThrowIfNull(convention);

        GroupConventions.Add(convention);
        return this;
    }

    /// <summary>
    /// Adds a convention applied to every route group that is a <typeparamref name="TGroup"/>,
    /// typically a marker interface of your own such as <c>IAdminEndpointGroup</c>.
    /// </summary>
    /// <param name="convention">The convention, given the group and its builder.</param>
    /// <typeparam name="TGroup">The groups the convention applies to.</typeparam>
    /// <returns>The options, for chaining.</returns>
    public EndpointMappingOptions ConfigureGroups<TGroup>(
        Action<TGroup, RouteGroupBuilder> convention)
        where TGroup : class
    {
        ArgumentNullException.ThrowIfNull(convention);

        GroupConventions.Add(context =>
        {
            if (context.Group is TGroup group)
            {
                convention(group, context.Builder);
            }
        });
        return this;
    }

    /// <summary>
    /// Adds a convention applied to every endpoint.
    /// </summary>
    /// <param name="convention">The convention.</param>
    /// <returns>The options, for chaining.</returns>
    public EndpointMappingOptions ConfigureEndpoints(Action<EndpointContext> convention)
    {
        ArgumentNullException.ThrowIfNull(convention);

        EndpointConventions.Add(convention);
        return this;
    }
}
