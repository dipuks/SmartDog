using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDog.Domain.Models;

namespace SmartDog.Infrastructure.Persistence.Configurations;

public class DecisionCriterionConfiguration
    : IEntityTypeConfiguration<DecisionCriterion>
{
    public void Configure(EntityTypeBuilder<DecisionCriterion> builder)
    {
        builder.ToTable("DecisionCriteria");

        builder.HasKey(criterion => criterion.Id);
        builder.HasAlternateKey(criterion => new
        {
            criterion.Id,
            criterion.DecisionId
        });

        builder.Property(criterion => criterion.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(criterion => criterion.Weight)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.HasOne(criterion => criterion.Decision)
            .WithMany(decision => decision.Criteria)
            .HasForeignKey(criterion => criterion.DecisionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}