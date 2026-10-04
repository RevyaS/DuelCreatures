using System.Collections.Generic;
using MessagePack;

[MessagePackObject]
public class DeckInfo
{
    [Key(0)]
    public string Name { get; set; } = null!;
    [Key(1)]
    public string FirstVanguardName { get; set; } = null!;
    [Key(2)]
    public Dictionary<string, int> Cards { get; set; } = new();
}