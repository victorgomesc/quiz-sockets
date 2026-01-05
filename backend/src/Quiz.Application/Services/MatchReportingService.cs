using Quiz.Application.Abstractions;
using Quiz.Domain.Entities;

namespace Quiz.Application.Services;

public sealed class MatchReportingService
{
    private readonly IMatchRepository _matches;
    private readonly IRankingRepository _ranking;

    public MatchReportingService(IMatchRepository matches, IRankingRepository ranking)
    {
        _matches = matches;
        _ranking = ranking;
    }

    public async Task ReportAsync(string roomCode, DateTime startedAtUtc, DateTime endedAtUtc, List<PlayerResult> players)
    {
        // winner = maior score (empate: primeira ocorrência)
        var maxScore = players.Max(p => p.Score);
        var winners = players.Where(p => p.Score == maxScore).Select(p => p.UserId).ToHashSet();

        var match = new Match(roomCode, startedAtUtc, endedAtUtc);

        foreach (var p in players)
        {
            match.Participants.Add(new MatchParticipant(match.Id, p.UserId, p.Score, p.CorrectAnswers, p.TotalAnswers));
        }

        await _matches.AddAsync(match);

        // atualiza ranking (UserStats)
        foreach (var p in players)
        {
            var stats = await _ranking.GetOrCreateAsync(p.UserId);
            stats.ApplyMatch(p.Score, winners.Contains(p.UserId));
            await _ranking.SaveAsync(stats);
        }
    }
}

public sealed class PlayerResult
{
    public Guid UserId { get; set; }
    public int Score { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalAnswers { get; set; }
}
