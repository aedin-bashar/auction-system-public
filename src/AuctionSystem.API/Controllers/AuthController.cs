using AuctionSystem.API.DTOs;
using AuctionSystem.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuctionSystem.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var user = await _authService.Register(dto.Username, dto.Password, dto.Role);
        if (user == null) return BadRequest("Username already exists");
        return Ok(new { user.Id, user.Username, user.Role });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var token = await _authService.Login(dto.Username, dto.Password);
        if (token == null) return Unauthorized("Invalid credentials");
        return Ok(new { token });
    }
}
