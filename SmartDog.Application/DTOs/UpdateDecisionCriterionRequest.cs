using System.ComponentModel.DataAnnotations;

namespace SmartDog.Application.DTOs;

public record UpdateDecisionCriterionRequest(
    [Required]
    [StringLength(200, MinimumLength = 1)]
    string Name,

    [Range(typeof(decimal), "0", "100")]
    decimal Weight);
