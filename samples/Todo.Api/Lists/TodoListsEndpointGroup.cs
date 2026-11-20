using Hlibz.MinimalApi.Endpoints;

namespace Todo.Api.Lists;

internal sealed class TodoListsEndpointGroup : IEndpointGroup
{
    public string Prefix => "api/lists";

    public void Configure(RouteGroupBuilder group) => group.WithTags("Lists");
}
