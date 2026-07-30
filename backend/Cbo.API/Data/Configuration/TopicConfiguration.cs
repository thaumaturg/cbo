using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cbo.API.Data.Configuration;

public class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> entity)
    {
        entity.HasKey(t => t.Id);

        entity.Property(t => t.Id)
            .HasDefaultValueSql("uuidv7()");

        entity.Property(t => t.Title)
            .IsRequired();

        // One-to-many: Topic -> Questions
        entity.HasMany(t => t.Questions)
            .WithOne(q => q.Topic)
            .HasForeignKey(q => q.TopicId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-many: Topic -> TournamentTopics
        entity.HasMany(t => t.TournamentTopics)
            .WithOne(tt => tt.Topic)
            .HasForeignKey(tt => tt.TopicId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-many: Topic -> TopicAuthors
        entity.HasMany(t => t.TopicAuthors)
            .WithOne(ta => ta.Topic)
            .HasForeignKey(ta => ta.TopicId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-many: Topic -> Rounds
        // Restrict: a played topic must not cascade-delete its rounds (played match data)
        entity.HasMany(t => t.Rounds)
            .WithOne(r => r.Topic)
            .HasForeignKey(r => r.TopicId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
