using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cbo.API.Data.Configuration;

public class TopicAuthorConfiguration : IEntityTypeConfiguration<TopicAuthor>
{
    public const string TopicIdApplicationUserIdIndexName = "ix_topic_authors_topic_id_application_user_id";

    public void Configure(EntityTypeBuilder<TopicAuthor> entity)
    {
        entity.HasKey(ta => ta.Id);

        entity.Property(ta => ta.IsOwner)
            .IsRequired();

        entity.Property(ta => ta.IsAuthor)
            .IsRequired();

        entity.Property(ta => ta.ApplicationUserId)
            .IsRequired();

        entity.Property(ta => ta.TopicId)
            .IsRequired();

        // A user is an author of a topic at most once; also stops two simultaneous adds of the same author
        entity.HasIndex(ta => new { ta.TopicId, ta.ApplicationUserId })
            .IsUnique()
            .HasDatabaseName(TopicIdApplicationUserIdIndexName);

        // Many-to-one: TopicAuthor -> ApplicationUser
        entity.HasOne(ta => ta.ApplicationUser)
            .WithMany(au => au.TopicAuthors)
            .HasForeignKey(ta => ta.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many-to-one: TopicAuthor -> Topic
        entity.HasOne(ta => ta.Topic)
            .WithMany(t => t.TopicAuthors)
            .HasForeignKey(ta => ta.TopicId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
