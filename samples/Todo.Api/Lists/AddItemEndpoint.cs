using Hlibz.MinimalApi.Endpoints;
using Hlibz.Results;
using Hlibz.Results.AspNetCore;

using Microsoft.AspNetCore.Mvc;

namespace Todo.Api.Lists;

/// <summary>
/// Adds an item to a to-do list. Answers <c>201 Created</c> with the new item and its URL in
/// <c>Location</c>.
/// </summary>
/// <remarks>
/// A POST is 200 by default; WithCreatedLocation makes this one 201 with a Location.
/// </remarks>
internal sealed class AddItemEndpoint : IEndpoint<TodoListsEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("{listId:int}/items", Handle)
            .WithCreatedLocation<TodoItem>(item => $"/api/lists/{item.ListId}/items/{item.Id}")
            .ProducesErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound);

    private static Result<TodoItem> Handle(
        TodoListId listId,
        [FromBody] AddItemRequest request,
        TodoStore store) =>
        store.AddItem(listId, request.Text);
}
