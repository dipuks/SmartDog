using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDog.Domain.Models;

namespace SmartDog.Infrastructure.Persistence.Configurations;

public class DecisionAlternativeConfiguration
    : IEntityTypeConfiguration<DecisionAlternative>
{
    public void Configure(
        EntityTypeBuilder<DecisionAlternative> builder)
    {
        builder.ToTable("DecisionAlternatives");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Description)
            .HasMaxLength(2000);

        builder.Property(a => a.Score)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.HasOne(a => a.Decision)
            .WithMany(d => d.Alternatives)
            .HasForeignKey(a => a.DecisionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}