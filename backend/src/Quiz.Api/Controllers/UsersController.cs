using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quiz.Api.Models.Users;
using Quiz.Application.Abstractions;

namespace Quiz.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserRepository _users;

    public UsersController(IUserRepository users) => _users = users;

    // Lista (protegido)
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<List<UserResponse>>> List([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        take = Math.Clamp(take, 1, 100);
        var list = await _users.ListAsync(skip, take);
        return list.Select(u => new UserResponse
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            CreatedAtUtc = u.CreatedAtUtc
        }).ToList();
    }

    // Meu perfil
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me()
    {
        var userId = GetUserId();
        var user = await _users.GetByIdAsync(userId);
        if (user is null) return NotFound();

        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            CreatedAtUtc = user.CreatedAtUtc
        };
    }

    // Atualizar meu perfil
    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(UpdateUserRequest req)
    {
        var userId = GetUserId();

        var user = await _users.GetByIdAsync(userId);
        if (user is null) return NotFound();

        // Carrega rastreado para update (simples: reconsulta rastreado)
        // Alternativa: mudar repo para tracking específico
        var tracked = await _users.GetByIdAsync(userId);
        if (tracked is null) return NotFound();

        // como GetByIdAsync está AsNoTracking, aqui vamos buscar pelo email/username rastreado
        // solução prática: re-hidratar via DbContext no repo (para manter simples, troque GetByIdAsync para tracking)
        // Para não “mexer demais”, vamos atualizar via método do repo com novo objeto:
        user.UpdateProfile(req.Username, req.Email);
        await _users.UpdateAsync(user);

        return NoContent();
    }

    // Trocar senha (me)
    [Authorize]
    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest req)
    {
        var userId = GetUserId();

        var user = await _users.GetByIdAsync(userId);
        if (user is null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            return BadRequest(new { code = "INVALID_PASSWORD" });

        user.SetPasswordHash(BCrypt.Net.BCrypt.HashPassword(req.NewPassword));
        await _users.UpdateAsync(user);

        return NoContent();
    }

    // Deletar minha conta
    [Authorize]
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe()
    {
        var userId = GetUserId();
        var user = await _users.GetByIdAsync(userId);
        if (user is null) return NotFound();

        await _users.DeleteAsync(user);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? User.FindFirstValue("sub");

        return Guid.Parse(sub!);
    }
}
