using Todo.Api.Rules;

namespace Todo.Api.Lists;

/// <summary>A to-do list.</summary>
/// <param name="Id">Gets the list's id.</param>
/// <param name="Title">Gets the list's title.</param>
/// <param name="Items">Gets the list's items, in the order they were added.</param>
public sealed record TodoList(TodoListId Id, string Title, IReadOnlyList<TodoItem> Items);

/// <summary>Something to do, on a to-do list.</summary>
/// <param name="Id">Gets the item's id.</param>
/// <param name="ListId">Gets the id of the list the item is on.</param>
/// <param name="Text">Gets what there is to do.</param>
/// <param name="IsDone">Gets a value indicating whether the item is done.</param>
public sealed record TodoItem(TodoItemId Id, TodoListId ListId, string Text, bool IsDone);

/// <summary>A new to-do list.</summary>
public sealed class CreateListRequest
{
    /// <summary>Gets the list's title.</summary>
    [NotBlank]
    public required string Title { get; init; }
}

/// <summary>A new item for a to-do list.</summary>
public sealed class AddItemRequest
{
    /// <summary>Gets what there is to do.</summary>
    [NotBlank]
    public required string Text { get; init; }
}
