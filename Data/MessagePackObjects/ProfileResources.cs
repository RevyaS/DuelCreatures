using MessagePack;

/// <summary>The game resources a profile holds (currencies, gacha tickets...).</summary>
[MessagePackObject]
public class ProfileResources
{
    [Key(0)]
    public int Tickets { get; set; }
}
