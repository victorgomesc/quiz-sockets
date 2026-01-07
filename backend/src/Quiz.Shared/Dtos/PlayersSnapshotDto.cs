namespace Quiz.Shared.Dtos;

public sealed class PlayersSnapshotDto
{
    public string RoomCode { get; set; } = default!;
    public List<PlayerStateDto> Players { get; set; } = new();
}
