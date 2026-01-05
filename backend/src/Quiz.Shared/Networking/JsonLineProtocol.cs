using System.Buffers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Quiz.Shared.Networking;

/// <summary>
/// Protocolo de transporte: 1 mensagem = 1 linha JSON (UTF-8) terminada por '\n'.
/// </summary>
public static class JsonLineProtocol
{
    private static readonly Encoding Utf8 = new UTF8Encoding(false);

    public static async Task WriteAsync(NetworkStream stream, MessageEnvelope envelope, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(envelope, JsonOptions.Default);
        var line = json + "\n";
        var bytes = Utf8.GetBytes(line);
        await stream.WriteAsync(bytes, 0, bytes.Length, ct);
        await stream.FlushAsync(ct);
    }

    public static async IAsyncEnumerable<MessageEnvelope> ReadAllAsync(
        NetworkStream stream,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        // Leitura incremental por linhas, sem StreamReader (para evitar buffering “surpresa”).
        var buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
        try
        {
            var sb = new StringBuilder();
            while (!ct.IsCancellationRequested)
            {
                int read = await stream.ReadAsync(buffer, ct);
                if (read <= 0) yield break;

                sb.Append(Utf8.GetString(buffer, 0, read));

                while (true)
                {
                    var s = sb.ToString();
                    var idx = s.IndexOf('\n');
                    if (idx < 0) break;

                    var line = s[..idx].Trim();
                    sb.Clear();
                    sb.Append(s[(idx + 1)..]);

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    MessageEnvelope? env;
                    try
                    {
                        env = JsonSerializer.Deserialize<MessageEnvelope>(line, JsonOptions.Default);
                    }
                    catch
                    {
                        // Linha inválida: ignore (ou poderia yield ERROR)
                        continue;
                    }

                    if (env is not null)
                        yield return env;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
