namespace SmartDog.Domain.Models;

public sealed class InvalidScoringConfigurationException : InvalidOperationException
{
    public InvalidScoringConfigurationException(string message)
        : base(message)
    {
    }
}
