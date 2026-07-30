using Cbo.API.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cbo.API.Data.Configuration;

public class RoundAnswerConfiguration : IEntityTypeConfiguration<RoundAnswer>
{
    public void Configure(EntityTypeBuilder<RoundAnswer> entity)
    {
        entity.HasKey(ra => ra.Id);

        entity.Property(ra => ra.Id)
            .HasDefaultValueSql("uuidv7()");

        entity.Property(ra => ra.RoundId)
            .IsRequired();

        entity.Property(ra => ra.QuestionId)
            .IsRequired();

        entity.Property(ra => ra.MatchParticipantId)
            .IsRequired();

        // Many-to-one: RoundAnswer -> Round
        entity.HasOne(ra => ra.Round)
            .WithMany(r => r.RoundAnswers)
            .HasForeignKey(ra => ra.RoundId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many-to-one: RoundAnswer -> Question
        // Restrict: deleting a question must not cascade-delete given answers (played match data)
        entity.HasOne(ra => ra.Question)
            .WithMany(q => q.RoundAnswers)
            .HasForeignKey(ra => ra.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Many-to-one: RoundAnswer -> MatchParticipant
        // Restrict: given answers (played match data) must not silently cascade away
        entity.HasOne(ra => ra.MatchParticipant)
            .WithMany(mp => mp.RoundAnswers)
            .HasForeignKey(ra => ra.MatchParticipantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
