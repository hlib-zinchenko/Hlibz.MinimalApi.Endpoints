using Hlibz.MinimalApi.Endpoints;
using Hlibz.Results;

namespace Todo.Api.Lists;

/// <summary>
/// Marks an item as done. Answers <c>200</c> with the updated item; an item already done is a
/// <c>409</c>.
/// </summary>
/// <remarks>A POST that creates nothing keeps the default, 200.</remarks>
internal sealed class CompleteItemEndpoint : IEndpoint<TodoListsEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("{listId:int}/items/{itemId:int}/complete", Handle)
            .ProducesErrors(StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

    private static Result<TodoItem> Handle(TodoListId listId, TodoItemId itemId, TodoStore store) =>
        store.CompleteItem(listId, itemId);
}
