using System.Collections.Concurrent;
using Quiz.Server.Networking;
using Quiz.Server.Reporting;
using Quiz.Shared.Dtos;
using Quiz.Shared.Networking;

namespace Quiz.Server.Rooms;

public sealed class GameLoop
{
    private readonly GameRoom _room;
    private readonly MatchReportClient _reportClient;

    private readonly ConcurrentDictionary<Guid, TimedAnswer> _answers = new();

    private sealed record TimedAnswer(
        AnswerDto Answer,
        DateTime ReceivedAtUtc
    );

    private DateTime _questionStartUtc;

    private readonly List<QuestionDto> _questions;
    private int _currentQuestionIndex = -1;

    private int CalculateScore(QuestionDto question, TimedAnswer timed)
    {
        if (timed.Answer.SelectedOptionIndex != question.CorrectOptionIndex)
            return 0;

        var elapsedMs = (timed.ReceivedAtUtc - _questionStartUtc).TotalMilliseconds;

        // Score máximo: 100, mínimo: 10
        var score = 100 - (int)(elapsedMs / 100);

        return Math.Clamp(score, 10, 100);
    }

    public GameLoop(GameRoom room, MatchReportClient reportClient)
    {
        _room = room;
        _reportClient = reportClient;

        _questions = new List<QuestionDto>
        {
            new()
            {
                QuestionId = "q1",
                Title = "Qual linguagem é usada no .NET?",
                Options = ["Java", "C#", "Python", "Go"],
                CorrectOptionIndex = 1
            },
            new()
            {
                QuestionId = "q2",
                Title = "TCP é orientado a?",
                Options = ["Mensagem", "Conexão", "Evento", "Pacote"],
                CorrectOptionIndex = 1
            }
        };
    }

    public async Task RunAsync()
    {
        foreach (var question in _questions)
        {
            _currentQuestionIndex++;
            _questionStartUtc = DateTime.UtcNow;
            _answers.Clear();

            // reset “respondeu?” e sincroniza snapshot
            _room.ResetAnswersForNewQuestion();
            await _room.BroadcastSnapshotAsync();

            var publicQuestion = new QuestionPublicDto
            {
                QuestionId = question.QuestionId,
                Title = question.Title,
                Options = question.Options
            };

            await _room.BroadcastAsync(
                MessageEnvelope.Create(MessageTypes.QUESTION, publicQuestion)
            );

            await Task.Delay(TimeSpan.FromSeconds(10));
        }

        await EndMatchAsync();
    }

    public bool ReceiveAnswer(ClientSession session, AnswerDto dto)
    {
        if (_currentQuestionIndex < 0)
            return false;

        if (session.UserId is null)
            return false;

        var userId = session.UserId.Value;

        // uma resposta por usuário por pergunta
        var ok = _answers.TryAdd(
            userId,
            new TimedAnswer(dto, DateTime.UtcNow)
        );

        if (!ok)
            return false;

        var question = _questions[_currentQuestionIndex];
        var timed = _answers[userId];
        var delta = CalculateScore(question, timed);
        var isCorrect = delta > 0;

        // marca como respondeu e atualiza placar acumulado
        _room.MarkAnswered(userId);
        var total = _room.AddScore(userId, delta);

        // 1) broadcast: “fulano respondeu” (status)
        _ = _room.BroadcastAsync(MessageEnvelope.Create(
            MessageTypes.ANSWER_RECEIVED,
            new { userId }
        ));

        // 2) broadcast: update de placar parcial
        _ = _room.BroadcastAsync(MessageEnvelope.Create(
            MessageTypes.SCORE_UPDATE,
            new ScoreUpdateDto { UserId = userId, Delta = delta, TotalScore = total }
        ));

        // 3) mensagem DIRETA para o jogador (feedback verde/vermelho)
        _ = session.SendAsync(MessageEnvelope.Create(
            MessageTypes.ANSWER_RESULT,
            new AnswerResultDto
            {
                QuestionId = dto.QuestionId,
                IsCorrect = isCorrect,
                Delta = delta,
                TotalScore = total
            },
            requestId: dto.QuestionId // opcional
        ), CancellationToken.None);

        // 4) snapshot atualizado (opcional, mas mantém UI sempre correta)
        _ = _room.BroadcastSnapshotAsync();

        return true;
    }

    private async Task EndMatchAsync()
    {
        _room.EndMatch();

        // Resultado final baseado no snapshot (score acumulado)
        var snapshot = _room.GetSnapshot();

        var results = snapshot.Select(p => new PlayerResult
        {
            UserId = p.UserId,
            Score = p.Score,
            CorrectAnswers = 0, // (opcional: você pode contar depois)
            TotalAnswers = _questions.Count
        }).ToList();

        await _room.BroadcastAsync(
            MessageEnvelope.Create(MessageTypes.MATCH_ENDED, results)
        );

        await _reportClient.ReportAsync(new MatchReportRequest
        {
            RoomCode = _room.Code,
            StartedAtUtc = _room.StartedAtUtc,
            EndedAtUtc = _room.EndedAtUtc!.Value,
            Players = results
        });
    }
}
