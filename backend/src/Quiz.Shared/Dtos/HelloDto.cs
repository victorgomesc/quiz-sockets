namespace Quiz.Shared.Dtos;

public sealed class HelloDto
{
    // Opcional: cliente pode enviar um userId persistente
    public string? UserId { get; set; }

    // Mensagem livre (debug / handshake)
    public string? Message { get; set; }

    // Sempre preenchido pelo servidor
    public DateTime ServerTimeUtc { get; set; }
}
