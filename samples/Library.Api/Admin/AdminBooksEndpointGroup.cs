namespace Library.Api.Admin;

internal sealed class AdminBooksEndpointGroup : IAdminEndpointGroup
{
    public string Prefix => "api/admin/books";

    public IReadOnlyList<string> Tags => ["Admin"];
}
