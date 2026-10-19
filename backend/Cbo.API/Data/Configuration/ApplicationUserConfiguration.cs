using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cbo.API.Data.Configuration;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    // Index names set by ASP.NET Core Identity
    public const string UserNameIndexName = "UserNameIndex";
    public const string EmailIndexName = "EmailIndex";

    public void Configure(EntityTypeBuilder<ApplicationUser> entity)
    {
        entity.Property(au => au.FullName)
            .HasMaxLength(64);

        // Identity makes only the user name unique. Logging in looks a user up by email, so an email belongs to one account
        // also stops two simultaneous registrations with the same email (RequireUniqueEmail checks it up front)
        entity.HasIndex(au => au.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName(EmailIndexName);

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
