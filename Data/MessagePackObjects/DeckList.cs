using System.Collections.Generic;
using MessagePack;

[MessagePackObject]
public class DeckList
{
    [Key(0)]
    public List<DeckInfo> List { get; set; } = null!;
}