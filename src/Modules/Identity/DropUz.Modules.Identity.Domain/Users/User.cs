using Microsoft.AspNetCore.Identity;

namespace DropUz.Modules.Identity.Domain.Users;

public class User : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? Code { get; set; }
    public string FullName => LastName + " " + FirstName;
}
