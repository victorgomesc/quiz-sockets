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

    // 🔽 CORRIGIDO: tipo correto
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
            _currentQuestionIndex++; // 🔽 ADICIONADO
            _questionStartUtc = DateTime.UtcNow;
            _answers.Clear();

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

        return _answers.TryAdd(
            session.UserId.Value,
            new TimedAnswer(dto, DateTime.UtcNow)
        );
    }

    private async Task EndMatchAsync()
    {
        _room.EndMatch();

        var results = new List<PlayerResult>();

        foreach (var (userId, timed) in _answers)
        {
            var question = _questions[_currentQuestionIndex];
            var score = CalculateScore(question, timed);

            results.Add(new PlayerResult
            {
                UserId = userId,
                Score = score,
                CorrectAnswers = score > 0 ? 1 : 0,
                TotalAnswers = _questions.Count
            });
        }

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
