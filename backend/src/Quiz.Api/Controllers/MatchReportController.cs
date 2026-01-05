using Microsoft.AspNetCore.Mvc;
using Quiz.Api.Models.Matches;
using Quiz.Application.Services;

namespace Quiz.Api.Controllers;

[ApiController]
[Route("api/internal/matches")]
public sealed class MatchReportController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly MatchReportingService _report;

    public MatchReportController(IConfiguration config, MatchReportingService report)
    {
        _config = config;
        _report = report;
    }

    [HttpPost("report")]
    public async Task<IActionResult> Report(MatchReportRequest req)
    {
        var headerKey = Request.Headers["X-Internal-Key"].ToString();
        var expected = _config["Internal:ReportKey"];

        if (string.IsNullOrWhiteSpace(expected) || headerKey != expected)
            return Unauthorized(new { code = "INVALID_INTERNAL_KEY" });

        await _report.ReportAsync(
            req.RoomCode,
            req.StartedAtUtc,
            req.EndedAtUtc,
            req.Players.Select(p => new PlayerResult
            {
                UserId = p.UserId,
                Score = p.Score,
                CorrectAnswers = p.CorrectAnswers,
                TotalAnswers = p.TotalAnswers
            }).ToList()
        );

        return Ok(new { ok = true });
    }
}
