namespace Onyx.IdP.Web.Features.Admin.Roles;

public class RolesViewModel
{
    public List<RoleDto> Roles { get; set; } = new();
    
    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    // Search and Sort
    public string? SearchTerm { get; set; }
    public string? SortColumn { get; set; }
    public string? SortDirection { get; set; }
}

public class RoleDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    
    public bool IsProtected => Name == "SuperAdmin" || Name == "User";
    public bool IsSuperAdmin => Name == "SuperAdmin";
    public int UserCount { get; set; }
}
