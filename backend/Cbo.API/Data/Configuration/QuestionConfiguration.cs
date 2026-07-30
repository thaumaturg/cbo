using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cbo.API.Data.Configuration;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> entity)
    {
        entity.HasKey(q => q.Id);

        entity.Property(q => q.Id)
            .HasDefaultValueSql("uuidv7()");

        entity.Property(q => q.QuestionNumber)
            .IsRequired();

        entity.Property(q => q.CostPositive)
            .IsRequired();

        entity.Property(q => q.CostNegative)
            .IsRequired();

        entity.Property(q => q.Text)
            .IsRequired();

        entity.Property(q => q.Answer)
            .IsRequired();

        entity.Property(q => q.TopicId)
            .IsRequired();

        // Many-to-one: Question -> Topic
        entity.HasOne(q => q.Topic)
            .WithMany(t => t.Questions)
            .HasForeignKey(q => q.TopicId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-many: Question -> RoundAnswers
        // Restrict: deleting a question must not cascade-delete given answers (played match data)
        entity.HasMany(q => q.RoundAnswers)
            .WithOne(ra => ra.Question)
            .HasForeignKey(ra => ra.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
