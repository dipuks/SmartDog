using System.ComponentModel.DataAnnotations;

namespace SmartDog.Application.DTOs;

public record UpdateDecisionRequest(
    [Required]
    [StringLength(200, MinimumLength = 3)]
    string Title,

    [StringLength(2000)]
    string? Description,

    [Required]
    [StringLength(50, MinimumLength = 1)]
    [RegularExpression("^(Draft|Created|InProgress|Completed|Cancelled)$")]
    string Status,

    [Required]
    [MinLength(8)]
    [MaxLength(8)]
    byte[] RowVersion);
