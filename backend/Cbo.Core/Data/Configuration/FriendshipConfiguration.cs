using Cbo.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cbo.Core.Data.Configuration;

public class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
    public const string ApplicationUserIdFriendIdIndexName = "ix_friendships_application_user_id_friend_id";

    public void Configure(EntityTypeBuilder<Friendship> entity)
    {
        entity.HasKey(f => f.Id);

        entity.Property(f => f.ApplicationUserId)
            .IsRequired();

        entity.Property(f => f.FriendId)
            .IsRequired();

        // An account is on a friend list at most once; also stops two simultaneous adds of the same friend
        entity.HasIndex(f => new { f.ApplicationUserId, f.FriendId })
            .IsUnique()
            .HasDatabaseName(ApplicationUserIdFriendIdIndexName);

        entity.ToTable(t => t.HasCheckConstraint("ck_friendships_not_self", "application_user_id <> friend_id"));

        // Many-to-one: Friendship -> ApplicationUser (the account whose list this is)
        entity.HasOne(f => f.ApplicationUser)
            .WithMany(au => au.Friendships)
            .HasForeignKey(f => f.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many-to-one: Friendship -> ApplicationUser (the friend), with no list of who added them
        entity.HasOne(f => f.Friend)
            .WithMany()
            .HasForeignKey(f => f.FriendId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
