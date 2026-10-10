using Microsoft.Extensions.DependencyInjection;
using SmartDog.Domain.Engines;

namespace SmartDog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddSmartDogApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IDecisionScoringEngine,
            WeightedDecisionScoringEngine>();

        services.AddScoped<Services.DecisionService>();

        return services;
    }
}