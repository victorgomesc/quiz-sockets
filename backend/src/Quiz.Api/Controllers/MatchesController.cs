using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quiz.Api.Models.Matches;
using Quiz.Application.Abstractions;

namespace Quiz.Api.Controllers;

[ApiController]
[Route("api/matches")]
public sealed class MatchesController : ControllerBase
{
    private readonly IMatchRepository _matches;

    public MatchesController(IMatchRepository matches) => _matches = matches;

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<List<MatchHistoryResponse>>> MyHistory([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        take = Math.Clamp(take, 1, 100);

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

        var list = await _matches.ListByUserAsync(userId, skip, take);

        return list.Select(m => new MatchHistoryResponse
        {
            MatchId = m.Id,
            RoomCode = m.RoomCode,
            StartedAtUtc = m.StartedAtUtc,
            EndedAtUtc = m.EndedAtUtc,
            Status = m.Status
        }).ToList();
    }
}
