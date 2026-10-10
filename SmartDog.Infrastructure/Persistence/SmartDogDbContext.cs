using Microsoft.EntityFrameworkCore;
using SmartDog.Domain.Models;

public class SmartDogDbContext : DbContext
{
    public SmartDogDbContext(
        DbContextOptions<SmartDogDbContext> options)
        : base(options)
    {
    }

    public DbSet<Decision> Decisions => Set<Decision>();

    public DbSet<User> Users => Set<User>();

    public DbSet<DecisionAlternative> DecisionAlternatives
    => Set<DecisionAlternative>();

    public DbSet<DecisionCriterion> DecisionCriteria
        => Set<DecisionCriterion>();

    public DbSet<DecisionAlternativeCriterionRating>
        DecisionAlternativeCriterionRatings
        => Set<DecisionAlternativeCriterionRating>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SmartDogDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}