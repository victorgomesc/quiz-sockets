using Microsoft.AspNetCore.Mvc;
using Quiz.Api.Models.Ranking;
using Quiz.Application.Abstractions;

namespace Quiz.Api.Controllers;

[ApiController]
[Route("api/ranking")]
public sealed class RankingController : ControllerBase
{
    private readonly IRankingRepository _ranking;

    public RankingController(IRankingRepository ranking) => _ranking = ranking;

    // Público por padrão (você pode colocar [Authorize] se quiser)
    [HttpGet("global")]
    public async Task<ActionResult<List<RankingEntryResponse>>> Global([FromQuery] int take = 20)
    {
        var top = await _ranking.TopAsync(take);
        return top.Select(x => new RankingEntryResponse
        {
            UserId = x.User.Id,
            Username = x.User.Username,
            TotalScore = x.Stats.TotalScore,
            MatchesPlayed = x.Stats.MatchesPlayed,
            Wins = x.Stats.Wins,
            UpdatedAtUtc = x.Stats.UpdatedAtUtc
        }).ToList();
    }
}
