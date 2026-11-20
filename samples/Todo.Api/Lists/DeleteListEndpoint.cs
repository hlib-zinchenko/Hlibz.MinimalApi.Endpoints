using Hlibz.MinimalApi.Endpoints;
using Hlibz.Results;

namespace Todo.Api.Lists;

/// <summary>
/// Deletes a to-do list and its items. Answers <c>204 No Content</c>.
/// </summary>
/// <remarks>204 comes from the app-wide default for DELETE, set in Program.cs.</remarks>
internal sealed class DeleteListEndpoint : IEndpoint<TodoListsEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapDelete("{listId:int}", Handle)
            .ProducesErrors(StatusCodes.Status404NotFound);

    private static Result Handle(TodoListId listId, TodoStore store) => store.DeleteList(listId);
}
