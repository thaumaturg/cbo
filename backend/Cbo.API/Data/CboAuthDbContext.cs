using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Data;

public class CboAuthDbContext : IdentityDbContext
{
    public CboAuthDbContext(DbContextOptions<CboAuthDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        string readerRoleId = "ad040ba3-7725-44e8-b7e0-0d9272be1792";
        string writerRoleId = "8994bf38-e92b-4fee-a86f-067f012b02f0";

        var roles = new List<IdentityRole>
        {
            new IdentityRole
            {
                Id = readerRoleId,
                ConcurrencyStamp = readerRoleId,
                Name = "Reader",
                NormalizedName = "Reader".ToUpper()
            },
            new IdentityRole
            {
                Id = "2",
                ConcurrencyStamp = writerRoleId,
                Name = writerRoleId,
                NormalizedName = "Writer".ToUpper()
            },
        };

        modelBuilder.Entity<IdentityRole>().HasData(roles);
    }
}
