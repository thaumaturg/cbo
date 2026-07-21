using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cbo.API.Data.Configuration;

public class TopicAuthorConfiguration : IEntityTypeConfiguration<TopicAuthor>
{
    public void Configure(EntityTypeBuilder<TopicAuthor> entity)
    {
        entity.HasKey(ta => ta.Id);

        entity.Property(ta => ta.Id)
            .HasDefaultValueSql("uuidv7()");

        entity.Property(ta => ta.IsOwner)
            .IsRequired();

        entity.Property(ta => ta.IsAuthor)
            .IsRequired();

        entity.Property(ta => ta.ApplicationUserId)
            .IsRequired();

        entity.Property(ta => ta.TopicId)
            .IsRequired();

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
