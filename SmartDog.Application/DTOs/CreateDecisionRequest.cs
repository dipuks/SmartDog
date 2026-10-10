using System.ComponentModel.DataAnnotations;

namespace SmartDog.Application.DTOs;

public record CreateDecisionRequest(
    [Required]
    [StringLength(200, MinimumLength = 3)]
    string Title,

    [StringLength(2000)]
    string? Description);