using Cbo.Core.Domain;
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
        options.Password.RequiredLength = Limits.Accounts.PasswordLengthMin;
        options.Password.RequiredUniqueChars = 1;

        options.User.RequireUniqueEmail = true;
        // Login tells usernames from emails by '@', see docs/adr/0005-auth-on-dedicated-pages.md
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";
    }
}
