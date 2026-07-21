using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cbo.API.Data.Configuration;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> entity)
    {
        entity.Property(au => au.FullName)
            .HasMaxLength(64);

        // One-to-many: ApplicationUser -> TournamentParticipants
        entity.HasMany(au => au.TournamentParticipants)
            .WithOne(tp => tp.ApplicationUser)
            .HasForeignKey(tp => tp.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-many: ApplicationUser -> TopicAuthors
        entity.HasMany(au => au.TopicAuthors)
            .WithOne(ta => ta.ApplicationUser)
            .HasForeignKey(ta => ta.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
