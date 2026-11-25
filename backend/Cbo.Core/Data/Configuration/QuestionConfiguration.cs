using Cbo.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cbo.Core.Data.Configuration;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> entity)
    {
        entity.HasKey(q => q.Id);

        entity.Property(q => q.QuestionNumber)
            .IsRequired();

        entity.Property(q => q.Cost)
            .IsRequired();

        entity.Property(q => q.Text)
            .IsRequired()
            .HasMaxLength(Limits.Topics.QuestionTextLengthMax);

        entity.Property(q => q.Answer)
            .IsRequired()
            .HasMaxLength(Limits.Topics.AnswerLengthMax);

        entity.Property(q => q.Comment)
            .HasMaxLength(Limits.Topics.CommentLengthMax);

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
