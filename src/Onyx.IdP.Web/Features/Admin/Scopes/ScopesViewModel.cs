namespace Onyx.IdP.Web.Features.Admin.Scopes;

public class ScopesViewModel
{
    public List<ScopeDto> Scopes { get; set; } = new();

    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public long TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    // Search and Sort
    public string? SearchTerm { get; set; }
    public string? SortColumn { get; set; }
    public string? SortDirection { get; set; }
}

public class ScopeDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string Resources { get; set; } = string.Empty; // Space separated
}

public class CreateScopeViewModel
{
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? Resources { get; set; } // Space separated input
}
