using System.ComponentModel.DataAnnotations;

namespace SmartDog.Application.DTOs;

public record UpdateDecisionAlternativeRequest(
    [Required]
    [StringLength(200, MinimumLength = 1)]
    string Name,

    [StringLength(2000)]
    string? Description,

    [Range(0, 100)]
    decimal Score);