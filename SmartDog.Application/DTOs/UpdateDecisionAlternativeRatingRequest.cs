using System.ComponentModel.DataAnnotations;

namespace SmartDog.Application.DTOs;

public record UpdateDecisionAlternativeRatingRequest(
    [Range(typeof(decimal), "0", "100")]
    decimal Rating);
