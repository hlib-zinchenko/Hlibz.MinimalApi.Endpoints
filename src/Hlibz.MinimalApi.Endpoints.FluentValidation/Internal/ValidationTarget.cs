using System.Reflection;

namespace Hlibz.MinimalApi.Endpoints.FluentValidation.Internal;

/// <summary>
/// One value a handler receives that has a validator registered: the argument at a position, or
/// a property of the <c>[AsParameters]</c> argument there. Found once per endpoint, when it is
/// built, so a request only reads the value and validates it.
/// </summary>
internal abstract class ValidationTarget
{
    private readonly int _argumentIndex;

    private readonly PropertyInfo? _property;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationTarget"/> class.
    /// </summary>
    /// <param name="argumentIndex">The handler parameter's position.</param>
    /// <param name="property">
    /// The property of the <c>[AsParameters]</c> argument holding the value, or
    /// <see langword="null"/> when the argument itself is the value.
    /// </param>
    protected ValidationTarget(int argumentIndex, PropertyInfo? property)
    {
        _argumentIndex = argumentIndex;
        _property = property;
    }

    /// <summary>
    /// Finds the values of <paramref name="handler"/>'s arguments that have a validator
    /// registered in <paramref name="services"/>.
    /// </summary>
    /// <param name="handler">The endpoint's handler.</param>
    /// <param name="services">The app's services.</param>
    /// <returns>The values to validate, in parameter order; empty when there are none.</returns>
    public static ValidationTarget[] For(MethodInfo handler, IServiceProvider services)
    {
        IServiceProviderIsService isService =
            services.GetRequiredService<IServiceProviderIsService>();
        bool HasValidator(Type type) =>
            CanBeValidated(type)
            && isService.IsService(typeof(IValidator<>).MakeGenericType(type));

        List<ValidationTarget> targets = [];
        ParameterInfo[] parameters = handler.GetParameters();
        for (int index = 0; index < parameters.Length; index++)
        {
            ParameterInfo parameter = parameters[index];
            if (HasValidator(parameter.ParameterType))
            {
                targets.Add(Create(parameter.ParameterType, index, property: null));
            }

            if (!parameter.IsDefined(typeof(AsParametersAttribute)))
            {
                continue;
            }

            // An [AsParameters] type's properties bind on their own (a [FromBody] inside one,
            // say), so their types count as much as the handler's parameters.
            foreach (PropertyInfo property in parameter.ParameterType
                         .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         .Where(property => property.CanRead
                                            && property.GetIndexParameters().Length == 0
                                            && HasValidator(property.PropertyType)))
            {
                targets.Add(Create(property.PropertyType, index, property));
            }
        }

        return [.. targets];
    }

    /// <summary>
    /// Gets the value to validate from the handler's arguments.
    /// </summary>
    /// <param name="arguments">The handler's arguments.</param>
    /// <returns>The value, or <see langword="null"/> when there is none.</returns>
    public object? ValueIn(IList<object?> arguments)
    {
        object? argument = arguments[_argumentIndex];
        return _property is null || argument is null ? argument : _property.GetValue(argument);
    }

    /// <summary>
    /// Validates <paramref name="value"/> with the validator from the request's services.
    /// </summary>
    /// <param name="value">The value, from <see cref="ValueIn"/>.</param>
    /// <param name="httpContext">The request.</param>
    /// <returns>The result.</returns>
    public abstract Task<ValidationResult> ValidateAsync(object value, HttpContext httpContext);

    private static ValidationTarget Create(Type type, int argumentIndex, PropertyInfo? property) =>
        (ValidationTarget)Activator.CreateInstance(
            typeof(ValidationTarget<>).MakeGenericType(type),
            argumentIndex,
            property)!;

    /// <summary>
    /// Whether <paramref name="type"/> can be a generic argument at all: by-ref, pointer and
    /// ref struct parameters (<c>ref</c>/<c>out</c>, <c>Span&lt;T&gt;</c>) can't.
    /// </summary>
    private static bool CanBeValidated(Type type) =>
        type is
        {
            IsByRef: false,
            IsPointer: false,
            IsByRefLike: false,
            ContainsGenericParameters: false,
        };
}

/// <summary>
/// A <see cref="ValidationTarget"/> of type <typeparamref name="T"/>, validated with
/// <see cref="IValidator{T}"/>.
/// </summary>
/// <typeparam name="T">The validated type.</typeparam>
internal sealed class ValidationTarget<T> : ValidationTarget
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationTarget{T}"/> class.
    /// </summary>
    /// <param name="argumentIndex">The handler parameter's position.</param>
    /// <param name="property">The <c>[AsParameters]</c> property, or <see langword="null"/>.</param>
    public ValidationTarget(int argumentIndex, PropertyInfo? property)
        : base(argumentIndex, property)
    {
    }

    /// <inheritdoc/>
    public override Task<ValidationResult> ValidateAsync(object value, HttpContext httpContext) =>
        httpContext.RequestServices
            .GetRequiredService<IValidator<T>>()
            .ValidateAsync((T)value, httpContext.RequestAborted);
}
