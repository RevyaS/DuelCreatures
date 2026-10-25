using System;
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

/// <summary>
/// The game's data: which card pool is played with, the profile in use, and what a new profile starts with.
/// Reads and writes through the <see cref="RevDataManager"/> autoload, so it has to be loaded after it.
/// </summary>
public partial class DataManager : Node
{
    public const string GameDataDirectory = "Data/GameData";
    public const CardPoolType DefaultCardPool = CardPoolType.BALANCED;
    public static readonly SaveSlotLayout ProfileSlots = new("user://Profiles", 5, "Profile");

    /// <summary>Every new profile owns these decks, and the cards in them.</summary>
    public static IReadOnlyList<(string Name, VanguardDeckBuilder Deck)> InitialDecks =>
    [
        ("Royal Paladin Trial Deck", DeckFactory.RoyalPaladinTrialDeck),
        ("Kagero Trial Deck", DeckFactory.KageroTrialDeck),
        ("Oracle Think Tank Trial Deck", DeckFactory.OTTTrialDeck),
        ("Nova Grappler Trial Deck", DeckFactory.NovaGrapplerTrialDeck)
    ];

    RevDataManager data = null!;
    CardCatalog? cards;
    CardPoolType cardPool = DefaultCardPool;

    public event Action? ProfileChanged;

    public Profile? CurrentProfile { get; private set; }
    public SaveSlot? CurrentSlot { get; private set; }

    /// <summary>Loaded on first use, the card pools only exist once the DataGenerator has run.</summary>
    public CardCatalog Cards => cards ??= new CardCatalog(cardPool, data.Load<CardPool>(GetCardPoolPath(cardPool)));

    public CardPoolType CardPool
    {
        get => cardPool;
        set
        {
            if(cardPool == value) return;
            cardPool = value;
            cards = null;
        }
    }

    public static DataManager Get(Node node) => node.GetTree().Root.GetNode<DataManager>(nameof(DataManager));

    public static string GetCardPoolPath(CardPoolType poolType) => poolType switch
    {
        CardPoolType.ORIGINAL => $"{GameDataDirectory}/CardPool.Original.dat",
        CardPoolType.BALANCED => $"{GameDataDirectory}/CardPool.Balanced.dat",
        _ => throw new NotSupportedException($"Card Pool Not Supported {poolType}")
    };

    public override void _Ready()
    {
        data = RevDataManager.Get(this);
    }

    public IReadOnlyList<SaveSlot> GetProfileSlots() => data.GetSlots(ProfileSlots);

    public Profile ReadProfile(SaveSlot slot) => data.Load<Profile>(slot.Path);

    public static Profile CreateInitialProfile(string name)
    {
        var profile = new Profile
        {
            Name = name,
            CreatedAt = DateTime.Now,
            Decks = InitialDecks.Select(deck => DeckInfo.From(deck.Name, deck.Deck)).ToList()
        };
        profile.Decks.ForEach(profile.AddCards);
        return profile;
    }

    /// <summary>Creates a new profile in the slot, replacing whatever was there, and uses it.</summary>
    public Profile CreateProfile(SaveSlot slot, string name)
    {
        var profile = CreateInitialProfile(name);
        data.Save(slot.Path, profile);
        UseProfile(data.ResolveLayout(ProfileSlots).GetSlot(slot.Index), profile);
        return profile;
    }

    public Profile LoadProfile(SaveSlot slot)
    {
        var profile = ReadProfile(slot);
        UseProfile(slot, profile);
        return profile;
    }

    public void SaveProfile()
    {
        if(CurrentProfile is null || CurrentSlot is null) throw new InvalidOperationException("No profile is loaded");
        data.Save(CurrentSlot.Path, CurrentProfile);
    }

    private void UseProfile(SaveSlot slot, Profile profile)
    {
        CurrentSlot = slot;
        CurrentProfile = profile;
        ProfileChanged?.Invoke();
    }
}
