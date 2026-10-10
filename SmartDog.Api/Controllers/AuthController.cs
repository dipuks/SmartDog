using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartDog.Application.DTOs;
using SmartDog.Application.Services;

namespace SmartDog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService
        _authenticationService;

    public AuthController(
        IAuthenticationService authenticationService)
    {
        _authenticationService =
            authenticationService;
    }

    [EnableRateLimiting("login")]
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request)
    {
        var result =
            await _authenticationService.LoginAsync(request);

        if (result is null)
        {
            return Unauthorized();
        }

        return Ok(result);
    }
}