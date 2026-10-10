using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDog.Domain.Models;

namespace SmartDog.Infrastructure.Persistence.Configurations;

public class DecisionAlternativeCriterionRatingConfiguration
    : IEntityTypeConfiguration<DecisionAlternativeCriterionRating>
{
    public void Configure(
        EntityTypeBuilder<DecisionAlternativeCriterionRating> builder)
    {
        builder.ToTable("DecisionAlternativeCriterionRatings");

        builder.HasKey(rating => new
        {
            rating.DecisionId,
            rating.DecisionAlternativeId,
            rating.DecisionCriterionId
        });

        builder.Property(rating => rating.Rating)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.HasOne(rating => rating.Alternative)
            .WithMany(alternative => alternative.CriterionRatings)
            .HasForeignKey(rating => new
            {
                rating.DecisionAlternativeId,
                rating.DecisionId
            })
            .HasPrincipalKey(alternative => new
            {
                alternative.Id,
                alternative.DecisionId
            })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rating => rating.Criterion)
            .WithMany(criterion => criterion.Ratings)
            .HasForeignKey(rating => new
            {
                rating.DecisionCriterionId,
                rating.DecisionId
            })
            .HasPrincipalKey(criterion => new
            {
                criterion.Id,
                criterion.DecisionId
            })
            .OnDelete(DeleteBehavior.NoAction);
    }
}