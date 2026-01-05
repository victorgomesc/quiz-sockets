using Microsoft.AspNetCore.Mvc;
using Quiz.Api.Models.Auth;
using Quiz.Api.Security;
using Quiz.Application.Abstractions;
using Quiz.Domain.Entities;

namespace Quiz.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly JwtTokenService _jwt;

    public AuthController(IUserRepository users, JwtTokenService jwt)
    {
        _users = users;
        _jwt = jwt;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);
        var user = new User(req.Username, req.Email, hash);

        await _users.AddAsync(user);

        return Ok();
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var user = await _users.GetByEmailAsync(req.Email);
        if (user is null)
            return Unauthorized();

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized();

        var token = _jwt.Generate(user);

        return new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Username = user.Username
        };
    }
}
