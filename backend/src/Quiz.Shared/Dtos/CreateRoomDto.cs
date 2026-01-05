namespace Quiz.Shared.Dtos;

public sealed class CreateRoomDto
{
    // Nome opcional da sala (pode ser exibido no front)
    public string? RoomName { get; set; }

    // Quantidade máxima de jogadores (opcional)
    public int? MaxPlayers { get; set; }
}
