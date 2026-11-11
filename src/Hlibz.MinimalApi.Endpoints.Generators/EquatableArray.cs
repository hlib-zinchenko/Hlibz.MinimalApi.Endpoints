using System.Collections;

namespace Hlibz.MinimalApi.Endpoints.Generators;

/// <summary>
/// An array compared by its items, so the models holding one compare by value in the incremental
/// pipeline.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
    where T : IEquatable<T>
{
    private readonly T[]? _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="EquatableArray{T}"/> struct.
    /// </summary>
    /// <param name="items">The items, owned by the new instance from now on.</param>
    public EquatableArray(T[] items)
    {
        _items = items;
    }

    /// <inheritdoc/>
    public int Count => _items?.Length ?? 0;

    /// <inheritdoc/>
    public T this[int index] => (_items ?? [])[index];

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) =>
        left.Equals(right);

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) =>
        !left.Equals(right);

    /// <inheritdoc/>
    public bool Equals(EquatableArray<T> other) =>
        (_items ?? []).SequenceEqual(other._items ?? []);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        int hash = 17;
        foreach (T item in _items ?? [])
        {
            hash = unchecked((hash * 31) + item.GetHashCode());
        }

        return hash;
    }

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)(_items ?? [])).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
