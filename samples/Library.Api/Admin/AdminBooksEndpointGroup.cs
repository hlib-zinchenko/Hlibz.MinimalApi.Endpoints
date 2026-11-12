namespace Library.Api.Admin;

internal sealed class AdminBooksEndpointGroup : IAdminEndpointGroup
{
    public string Prefix => "api/admin/books";

    public void Configure(RouteGroupBuilder group) => group.WithTags("Admin");
}
