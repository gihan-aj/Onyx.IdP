namespace Onyx.IdP.Web.Features.Admin.Users;

public class UsersViewModel
{
    public List<UserDto> Users { get; set; } = new();
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public string? SortColumn { get; set; }
    public string? SortDirection { get; set; }

    public int TotalPages => (int)Math.Ceiling(decimal.Divide(TotalCount, PageSize));
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsLockedOut { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();

    public string FullName => $"{FirstName} {LastName}".Trim();
}
