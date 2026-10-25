using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Dev scene, run it from the editor whenever cards are added to the factory. Regenerates the original card pool,
/// and adds new cards to the balanced pool while keeping the stats already edited there.
/// </summary>
public partial class DataGenerator : Control
{
    Label Log = null!;

    public override void _Ready()
    {
        Log = GetNode<Label>($"%{nameof(Log)}");
        GenerateCardPools();
    }

    private void GenerateCardPools()
    {
        var data = RevDataManager.Get(this);

        var original = new CardPool
        {
            Cards = CardRegistry.Ids.Select(id => CardData.From(id, CardRegistry.Create(id))).ToList()
        };
        data.Save(DataManager.GetCardPoolPath(CardPoolType.ORIGINAL), original);
        Write($"Original card pool: {original.Cards.Count} cards");

        string balancedPath = DataManager.GetCardPoolPath(CardPoolType.BALANCED);
        Dictionary<string, CardData> edited = data.TryLoad<CardPool>(balancedPath, out var existing)
            ? existing.Cards.ToDictionary(card => card.Id)
            : new();

        var balanced = new CardPool
        {
            Cards = original.Cards.Select(card => edited.GetValueOrDefault(card.Id) ?? card).ToList()
        };
        data.Save(balancedPath, balanced);

        var added = original.Cards.Where(card => !edited.ContainsKey(card.Id)).Select(card => card.Id).ToList();
        var removed = edited.Keys.Where(id => !CardRegistry.Contains(id)).ToList();
        Write($"Balanced card pool: {balanced.Cards.Count} cards, {added.Count} added, {removed.Count} removed");
        added.ForEach(id => Write($"  + {id}"));
        removed.ForEach(id => Write($"  - {id}"));
    }

    private void Write(string line)
    {
        GD.Print(line);
        Log.Text += line + "\n";
    }
}
