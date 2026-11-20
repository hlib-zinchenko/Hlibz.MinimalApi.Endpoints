using Hlibz.MinimalApi.Endpoints;
using Hlibz.Results;

namespace Todo.Api.Lists;

/// <summary>
/// Gets one to-do list with its items.
/// </summary>
/// <remarks>A GET keeps the default success status code, 200.</remarks>
internal sealed class GetListEndpoint : IEndpoint<TodoListsEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("{listId:int}", Handle)
            .ProducesErrors(StatusCodes.Status404NotFound);

    private static Result<TodoList> Handle(TodoListId listId, TodoStore store) => store.Find(listId);
}
