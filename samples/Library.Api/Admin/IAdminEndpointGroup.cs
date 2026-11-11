using Hlibz.MinimalApi.Endpoints;

namespace Library.Api.Admin;

/// <summary>
/// The app's own marker for admin groups: <c>Program.cs</c> gates every one of them with the
/// admin policy and checks its prefix.
/// </summary>
internal interface IAdminEndpointGroup : IEndpointGroup;
