using ArC.CardGames.Predefined.Vanguard;
using MessagePack;

/// <summary>
/// The stats of one card, keyed by its <see cref="CardRegistry"/> id. Skills can't be stored so they always come from the
/// factory, these values replace the factory card's stats. <see cref="Name"/> is only for reading the data:
/// skills find cards by name, so it is never applied.
/// </summary>
[MessagePackObject]
public class CardData
{
    [Key(0)]
    public string Id { get; set; } = null!;
    [Key(1)]
    public string Name { get; set; } = null!;
    [Key(2)]
    public int Grade { get; set; }
    [Key(3)]
    public int Power { get; set; }
    [Key(4)]
    public int Critical { get; set; }
    [Key(5)]
    public int Guard { get; set; }
    [Key(6)]
    public int Drives { get; set; }
    [Key(7)]
    public VanguardCardSkill Skill { get; set; }
    [Key(8)]
    public VanguardTrigger Trigger { get; set; }

    public static CardData From(string id, VanguardCard card) => new()
    {
        Id = id,
        Name = card.Name,
        Grade = card.Grade,
        Power = card.Power,
        Critical = card.Critical,
        Guard = card.Guard,
        Drives = card.Drives,
        Skill = card.Skill,
        Trigger = card.Trigger
    };

    public bool HasSameStats(CardData other) =>
        Grade == other.Grade && Power == other.Power && Critical == other.Critical && Guard == other.Guard
        && Drives == other.Drives && Skill == other.Skill && Trigger == other.Trigger;

    public VanguardCard ApplyTo(VanguardCard card) => card with
    {
        Grade = Grade,
        Power = Power,
        Critical = Critical,
        Guard = Guard,
        Drives = Drives,
        Skill = Skill,
        Trigger = Trigger
    };
}
