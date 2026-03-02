using API_SVsharp.Application.DTOs;
using API_SVsharp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_SVsharp.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;

    public AuthController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // Simulação temporária
        if (request.Username != "admin" || request.Password != "123")
            return Unauthorized();

        var token = _tokenService.GenerateToken("admin", "Admin");

        return Ok(new { token });
    }
}