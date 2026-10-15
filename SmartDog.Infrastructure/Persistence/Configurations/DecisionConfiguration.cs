using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDog.Domain.Models;

namespace SmartDog.Infrastructure.Persistence.Configurations;

public class DecisionConfiguration : IEntityTypeConfiguration<Decision>
{
    public void Configure(EntityTypeBuilder<Decision> builder)
    {
        builder.ToTable("Decisions");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Description)
            .HasMaxLength(2000);

        builder.Property(d => d.CreatedAt)
            .IsRequired();

        builder.Property(d => d.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Every query is scoped to the owner; this index also serves the owner's list ordered by creation time.
        builder.HasIndex(d => new { d.OwnerId, d.CreatedAt });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(d => d.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}