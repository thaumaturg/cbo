using Microsoft.AspNetCore.Identity;

namespace Cbo.Core.Accounts;

public static class AccountIdentityOptions
{
    public static void Configure(IdentityOptions options)
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
        options.Password.RequiredUniqueChars = 1;

        options.User.RequireUniqueEmail = true;
    }
}
