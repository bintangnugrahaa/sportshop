using Microsoft.AspNetCore.Identity;

namespace SportShop.Infrastructure.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";

    public ApplicationRole() : base()
    {
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
    }
}
