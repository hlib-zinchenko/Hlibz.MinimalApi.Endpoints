using Hlibz.MinimalApi.Endpoints;
using Hlibz.Results;
using Hlibz.Results.AspNetCore;

using Microsoft.AspNetCore.Mvc;

namespace Todo.Api.Lists;

/// <summary>
/// Creates a to-do list. Answers <c>201 Created</c> with the new list and its URL in
/// <c>Location</c>.
/// </summary>
/// <remarks>
/// A POST is 200 by default; WithCreatedLocation makes this one 201 with a Location.
/// </remarks>
internal sealed class CreateListEndpoint : IEndpoint<TodoListsEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost(string.Empty, Handle)
            .WithCreatedLocation<TodoList>(list => $"/api/lists/{list.Id}")
            .ProducesErrors(StatusCodes.Status400BadRequest);

    private static Result<TodoList> Handle([FromBody] CreateListRequest request, TodoStore store) =>
        store.CreateList(request.Title);
}
