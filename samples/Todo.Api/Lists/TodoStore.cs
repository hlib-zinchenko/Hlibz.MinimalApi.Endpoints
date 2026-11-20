using Hlibz.Results;

namespace Todo.Api.Lists;

/// <summary>
/// An in-memory store standing in for a database. Each use case returns a <see cref="Result"/>
/// that says what failed; the HTTP layer picks the status code.
/// </summary>
public sealed class TodoStore
{
    private readonly Lock _lock = new();

    private readonly Dictionary<TodoListId, TodoList> _lists = new()
    {
        [new(1)] = new TodoList(new(1), "Groceries", [new TodoItem(new(1), new(1), "Milk", IsDone: false)]),
    };

    private int _lastListId = 1;

    private int _lastItemId = 1;

    public Result<TodoList> Find(TodoListId listId)
    {
        lock (_lock)
        {
            return _lists.TryGetValue(listId, out TodoList? list)
                ? Result<TodoList>.Success(list)
                : ListNotFound<TodoList>(listId);
        }
    }

    public Result<TodoList> CreateList(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Blank<TodoList>(nameof(CreateListRequest.Title));
        }

        lock (_lock)
        {
            TodoList list = new(new TodoListId(++_lastListId), title, []);
            _lists[list.Id] = list;
            return Result<TodoList>.Success(list);
        }
    }

    public Result DeleteList(TodoListId listId)
    {
        lock (_lock)
        {
            return _lists.Remove(listId) ? Result.Success() : ListNotFound<TodoList>(listId);
        }
    }

    public Result<TodoItem> AddItem(TodoListId listId, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Blank<TodoItem>(nameof(AddItemRequest.Text));
        }

        lock (_lock)
        {
            if (!_lists.TryGetValue(listId, out TodoList? list))
            {
                return ListNotFound<TodoItem>(listId);
            }

            TodoItem item = new(new TodoItemId(++_lastItemId), listId, text, IsDone: false);
            _lists[listId] = list with { Items = [.. list.Items, item] };
            return Result<TodoItem>.Success(item);
        }
    }

    public Result<TodoItem> CompleteItem(TodoListId listId, TodoItemId itemId)
    {
        lock (_lock)
        {
            if (FindItem(listId, itemId) is not { } found)
            {
                return ItemNotFound<TodoItem>(listId, itemId);
            }

            if (found.Item.IsDone)
            {
                return Result<TodoItem>.Conflict($"Item {itemId} is already done.");
            }

            TodoItem done = found.Item with { IsDone = true };
            _lists[listId] = found.List with
            {
                Items = [.. found.List.Items.Select(item => item.Id == itemId ? done : item)],
            };
            return Result<TodoItem>.Success(done);
        }
    }

    public Result DeleteItem(TodoListId listId, TodoItemId itemId)
    {
        lock (_lock)
        {
            if (FindItem(listId, itemId) is not { } found)
            {
                return ItemNotFound<TodoItem>(listId, itemId);
            }

            _lists[listId] = found.List with
            {
                Items = [.. found.List.Items.Where(item => item.Id != itemId)],
            };
            return Result.Success();
        }
    }

    private static Result<T> Blank<T>(string field) =>
        Result<T>.Invalid(new Dictionary<string, string[]> { [field] = ["Must not be blank."] });

    private static Result<T> ListNotFound<T>(TodoListId listId) =>
        Result<T>.NotFound($"List {listId} was not found.");

    private static Result<T> ItemNotFound<T>(TodoListId listId, TodoItemId itemId) =>
        Result<T>.NotFound($"Item {itemId} was not found in list {listId}.");

    private (TodoList List, TodoItem Item)? FindItem(TodoListId listId, TodoItemId itemId) =>
        _lists.TryGetValue(listId, out TodoList? list)
        && list.Items.FirstOrDefault(item => item.Id == itemId) is { } item
            ? (list, item)
            : null;
}
