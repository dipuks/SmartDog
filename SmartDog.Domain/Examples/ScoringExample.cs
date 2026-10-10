using SmartDog.Domain.Engines;
using SmartDog.Domain.Models;

namespace SmartDog.Domain.Examples;

public static class ScoringExample
{
    public static void Run()
    {
        var alternatives = new List<DecisionAlternative>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Option A",
                Score = 85
            },

            new()
            {
                Id = Guid.NewGuid(),
                Name = "Option B",
                Score = 92
            },

            new()
            {
                Id = Guid.NewGuid(),
                Name = "Option C",
                Score = 76
            }
        };

        IDecisionScoringEngine engine =
            new WeightedDecisionScoringEngine();

        var results = engine.Score(alternatives);

        foreach (var result in results)
        {
            Console.WriteLine(
                $"{result.AlternativeName}: {result.Score}");
        }
    }
}