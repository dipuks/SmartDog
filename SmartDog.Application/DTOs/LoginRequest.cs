using System.ComponentModel.DataAnnotations;

namespace SmartDog.Application.DTOs;

public record LoginRequest(
    [Required]
    string Username,

    [Required]
    string Password);