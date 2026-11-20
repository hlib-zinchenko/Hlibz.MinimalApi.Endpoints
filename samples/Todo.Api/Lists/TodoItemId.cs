using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Todo.Api.Lists;

/// <summary>
/// A to-do item's id. Bound from the route through <see cref="TryParse"/> and written as a
/// JSON number, so Hlibz.OpenApi's <c>AddStronglyTypedIds()</c> documents it as the integer it
/// wraps rather than as an object.
/// </summary>
[JsonConverter(typeof(TodoItemIdJsonConverter))]
public readonly record struct TodoItemId(int Value) : IParsable<TodoItemId>
{
    public static TodoItemId Parse(string s, IFormatProvider? provider) =>
        new(int.Parse(s, NumberStyles.Integer, provider));

    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        out TodoItemId result)
    {
        bool parsed = int.TryParse(s, NumberStyles.Integer, provider, out int value);
        result = new TodoItemId(value);
        return parsed;
    }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

internal sealed class TodoItemIdJsonConverter : JsonConverter<TodoItemId>
{
    public override TodoItemId Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) =>
        new(reader.GetInt32());

    public override void Write(
        Utf8JsonWriter writer,
        TodoItemId value,
        JsonSerializerOptions options) =>
        writer.WriteNumberValue(value.Value);
}
