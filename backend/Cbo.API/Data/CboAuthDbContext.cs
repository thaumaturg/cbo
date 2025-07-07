using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Data;

public class CboAuthDbContext : IdentityDbContext
{
    public CboAuthDbContext(DbContextOptions<CboAuthDbContext> options) : base(options)
    {
    }
}
