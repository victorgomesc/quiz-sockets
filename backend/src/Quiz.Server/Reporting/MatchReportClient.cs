using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Quiz.Server.Reporting;

public sealed class MatchReportClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<MatchReportClient> _logger;

    public MatchReportClient(
        HttpClient http,
        IConfiguration config,
        ILogger<MatchReportClient> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task ReportAsync(MatchReportRequest request, CancellationToken ct = default)
{
    const int maxRetries = 3;
    var delay = TimeSpan.FromSeconds(2);

    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            var response = await SendAsync(request, ct);

            if (response)
                return;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Report attempt {Attempt} failed", attempt);
        }

        if (attempt < maxRetries)
        {
            await Task.Delay(delay, ct);
            delay *= 2; // exponential backoff
        }
    }

    _logger.LogError(
        "Match report permanently failed for room {RoomCode}",
        request.RoomCode
    );
}

private async Task<bool> SendAsync(MatchReportRequest request, CancellationToken ct)
{
    var baseUrl = _config["Api:BaseUrl"];
    var reportKey = _config["Api:ReportKey"];

    var httpRequest = new HttpRequestMessage(
        HttpMethod.Post,
        $"{baseUrl!.TrimEnd('/')}/api/internal/matches/report");

    httpRequest.Headers.Add("X-Internal-Key", reportKey);
    httpRequest.Content = JsonContent.Create(request);

    var response = await _http.SendAsync(httpRequest, ct);

    if (!response.IsSuccessStatusCode)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        _logger.LogWarning("Report failed {Status}: {Body}", response.StatusCode, body);
        return false;
    }

    _logger.LogInformation("Match report succeeded for room {RoomCode}", request.RoomCode);
    return true;
}

}
