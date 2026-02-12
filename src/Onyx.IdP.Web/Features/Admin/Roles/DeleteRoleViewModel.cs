namespace Onyx.IdP.Web.Features.Admin.Roles;

public class DeleteRoleViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int UserCount { get; set; }
    public bool UnassignUsers { get; set; }
}
