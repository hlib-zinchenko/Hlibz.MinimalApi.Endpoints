using Hlibz.MinimalApi.Endpoints;
using Hlibz.Results;

namespace Todo.Api.Lists;

/// <summary>
/// Removes an item from a to-do list. Answers <c>204 No Content</c>.
/// </summary>
/// <remarks>204 comes from the app-wide default for DELETE, set in Program.cs.</remarks>
internal sealed class DeleteItemEndpoint : IEndpoint<TodoListsEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapDelete("{listId:int}/items/{itemId:int}", Handle)
            .ProducesErrors(StatusCodes.Status404NotFound);

    private static Result Handle(TodoListId listId, TodoItemId itemId, TodoStore store) =>
        store.DeleteItem(listId, itemId);
}
