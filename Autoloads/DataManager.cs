using System;
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Predefined.Vanguard;
using ArC.CardGames.Predefined.Vanguard.Cards;
using Godot;

/// <summary>
/// The game's data: which card pool is played with, the profile in use, and what a new profile starts with.
/// Reads and writes through the <see cref="RevDataManager"/> autoload, so it has to be loaded after it.
/// </summary>
public partial class DataManager : Node
{
    public const string GameDataDirectory = "Data/GameData";
    public const CardPoolType DefaultCardPool = CardPoolType.ORIGINAL;
    /// <summary>The card factory the game plays with, swap it for the project's own factory. See <see cref="CardRegistry"/>.</summary>
    public static readonly Type CardFactory = typeof(VanguardCardsFactory);
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

    public bool HasCardPool => cards is not null || data.Exists(GetCardPoolPath(cardPool));

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
        // Decks saved before decks had a clan
        foreach(var deck in profile.Decks.Where(deck => deck.ClanId is null && deck.FirstVanguardId is not null))
        {
            deck.Clan = CardRegistry.GetClan(deck.FirstVanguardId!);
        }
        UseProfile(slot, profile);
        return profile;
    }

    public void SaveProfile()
    {
        if(CurrentProfile is null || CurrentSlot is null) throw new InvalidOperationException("No profile is loaded");
        data.Save(CurrentSlot.Path, CurrentProfile);
    }

    /// <summary>Saves the deck into the current profile and the profile to its slot. Returns the deck's index in the profile.</summary>
    public int SaveDeck(DeckInfo deck, int? deckIndex)
    {
        var profile = CurrentProfile ?? throw new InvalidOperationException("No profile is loaded");
        int index;
        if(deckIndex is int existing)
        {
            profile.Decks[existing] = deck;
            index = existing;
        }
        else
        {
            profile.Decks.Add(deck);
            index = profile.Decks.Count - 1;
        }
        SaveProfile();
        ProfileChanged?.Invoke();
        return index;
    }

    public void AddTicketToProfile(int amount)
    {
        var profile = CurrentProfile ?? throw new InvalidOperationException("No profile is loaded");
        profile.Resources.Tickets += amount;
        SaveProfile();
        ProfileChanged?.Invoke();
    }

    #region Gacha
    List<GachaSetFile>? gachaSetFiles;

    /// <summary>Every set json, read once.</summary>
    public IReadOnlyList<GachaSetFile> GachaSetFiles => gachaSetFiles ??= GachaSets.LoadAll();

    /// <summary>Problems per set file. Without <paramref name="checkImages"/> missing images are left out, the game shows placeholders for them.</summary>
    public Dictionary<string, List<string>> ValidateGachaSets(bool checkImages = true) => GachaSetValidator.ValidateAll(
        GachaSetFiles,
        cardId => HasCardPool ? Cards.Contains(cardId) : CardRegistry.Game.Contains(cardId),
        path => !checkImages || ResourceLoader.Exists(path));

    /// <summary>The set's box in the current profile, rolling a new one (and saving it) when needed.</summary>
    public GachaBox GetGachaBox(GachaSet set)
    {
        var profile = CurrentProfile ?? throw new InvalidOperationException("No profile is loaded");
        var existing = profile.GachaBoxes.GetValueOrDefault(set.Id);
        var box = GachaService.GetBox(profile, set, Random.Shared);
        if(box != existing) SaveProfile();
        return box;
    }

    public PackResult OpenPack(GachaSet set, int packIndex)
    {
        var profile = CurrentProfile ?? throw new InvalidOperationException("No profile is loaded");
        var result = GachaService.OpenPack(profile, set, packIndex);
        SaveProfile();
        ProfileChanged?.Invoke();
        return result;
    }
    #endregion

    public List<string> ValidateDeck(DeckInfo deck) => VanguardDeckRules.Validate(deck, Cards, CurrentProfile?.OwnedCards);

    public VanguardCard CreateCard(string cardId) => Cards.Create(cardId);

    private void UseProfile(SaveSlot slot, Profile profile)
    {
        CurrentSlot = slot;
        CurrentProfile = profile;
        ProfileChanged?.Invoke();
    }

    #region Play
    /// <summary>The profile's decks, or the trial decks when no profile is loaded.</summary>
    public IEnumerable<DeckChoice> GetDeckChoices()
    {
        if(CurrentProfile is null)
        {
            return InitialDecks.Select(deck => new DeckChoice(deck.Name, () => deck.Deck, true));
        }
        if(!HasCardPool)
        {
            return CurrentProfile.Decks.Select(deck => new DeckChoice($"{deck.Name} - no card pool generated", () => throw new InvalidOperationException("No card pool"), false));
        }
        return CurrentProfile.Decks.Select(deck => new DeckChoice(DescribeDeck(deck), () => Cards.BuildDeck(deck), ValidateDeck(deck).Count == 0));
    }

    public string DescribeDeck(DeckInfo deck)
    {
        bool playable = ValidateDeck(deck).Count == 0;
        return $"{deck.Name} ({deck.Clan.DisplayName}) {deck.CardCount}/{VanguardDeckRules.DeckSize}{(playable ? string.Empty : " - incomplete")}";
    }
    #endregion
}

/// <summary>A deck that can be picked to play, only built when the game starts.</summary>
public sealed record DeckChoice(string Label, Func<VanguardDeckBuilder> Build, bool Playable);