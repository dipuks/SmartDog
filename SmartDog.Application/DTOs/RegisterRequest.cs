using System.ComponentModel.DataAnnotations;

namespace SmartDog.Application.DTOs;

public sealed record RegisterRequest(
    [Required]
    [StringLength(100, MinimumLength = 3)]
    [RegularExpression(
        "^[a-zA-Z0-9._-]+$",
        ErrorMessage = "Username may contain letters, numbers, periods, underscores, and hyphens.")]
    string Username,

    [Required]
    [StringLength(128, MinimumLength = 12)]
    string Password);
