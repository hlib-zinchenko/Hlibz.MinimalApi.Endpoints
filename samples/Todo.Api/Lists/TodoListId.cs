using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Todo.Api.Lists;

/// <summary>
/// A to-do list's id. Bound from the route through <see cref="TryParse"/> and written as a
/// JSON number, so Hlibz.OpenApi's <c>AddStronglyTypedIds()</c> documents it as the integer it
/// wraps rather than as an object.
/// </summary>
[JsonConverter(typeof(TodoListIdJsonConverter))]
public readonly record struct TodoListId(int Value) : IParsable<TodoListId>
{
    public static TodoListId Parse(string s, IFormatProvider? provider) =>
        new(int.Parse(s, NumberStyles.Integer, provider));

    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        out TodoListId result)
    {
        bool parsed = int.TryParse(s, NumberStyles.Integer, provider, out int value);
        result = new TodoListId(value);
        return parsed;
    }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

internal sealed class TodoListIdJsonConverter : JsonConverter<TodoListId>
{
    public override TodoListId Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) =>
        new(reader.GetInt32());

    public override void Write(
        Utf8JsonWriter writer,
        TodoListId value,
        JsonSerializerOptions options) =>
        writer.WriteNumberValue(value.Value);
}
