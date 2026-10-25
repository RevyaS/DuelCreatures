using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Dev scene, run it from the editor. Generate regenerates the original card pool from <see cref="CardRegistry.Game"/>,
/// and adds new cards to the balanced pool while keeping the stats already edited there.
/// Validate compares the game's card pool against ArC's.
/// </summary>
public partial class DataGenerator : Control
{
    Label Log = null!;
    Button GenerateButton = null!, ValidateButton = null!;

    public override void _Ready()
    {
        Log = GetNode<Label>($"%{nameof(Log)}");
        GenerateButton = GetNode<Button>($"%{nameof(GenerateButton)}");
        GenerateButton.Pressed += GenerateCardPools;
        ValidateButton = GetNode<Button>($"%{nameof(ValidateButton)}");
        ValidateButton.Pressed += ValidateCardPool;
    }

    private void GenerateCardPools()
    {
        Log.Text = string.Empty;
        var data = RevDataManager.Get(this);

        var original = CardPoolValidator.ToPool(CardRegistry.Game);
        data.Save(DataManager.GetCardPoolPath(CardPoolType.ORIGINAL), original);
        Write($"Original card pool: {original.Cards.Count} cards from {CardRegistry.Game.FactoryType.Name}");

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
        var removed = edited.Keys.Where(id => !CardRegistry.Game.Contains(id)).ToList();
        Write($"Balanced card pool: {balanced.Cards.Count} cards, {added.Count} added, {removed.Count} removed");
        added.ForEach(id => Write($"  + {id}"));
        removed.ForEach(id => Write($"  - {id}"));
    }

    private void ValidateCardPool()
    {
        Log.Text = string.Empty;
        var data = RevDataManager.Get(this);

        Write($"{CardRegistry.Game.FactoryType.Name} compared to ArC's {CardRegistry.ArC.FactoryType.Name}");
        WriteComparison(CardPoolValidator.Compare(CardRegistry.ArC, CardRegistry.Game));

        var gamePool = CardPoolValidator.ToPool(CardRegistry.Game);
        foreach(var poolType in new[] { CardPoolType.ORIGINAL, CardPoolType.BALANCED })
        {
            Write(string.Empty);
            string path = DataManager.GetCardPoolPath(poolType);
            if(!data.TryLoad<CardPool>(path, out var pool))
            {
                Write($"{poolType} card pool: not generated yet ({path})");
                continue;
            }
            Write($"{poolType} card pool compared to {CardRegistry.Game.FactoryType.Name}");
            WriteComparison(CardPoolValidator.Compare(gamePool, pool));
        }
    }

    private void WriteComparison(CardPoolComparison comparison)
    {
        if(comparison.IsSame)
        {
            Write("  Same cards and stats");
            return;
        }
        Write($"  {comparison.Missing.Count} missing, {comparison.Added.Count} added, {comparison.Changed.Count} with different stats");
        foreach(var id in comparison.Missing) Write($"  - {id}");
        foreach(var id in comparison.Added) Write($"  + {id}");
        foreach(var id in comparison.Changed) Write($"  ~ {id}");
    }

    private void Write(string line)
    {
        GD.Print(line);
        Log.Text += line + "\n";
    }
}
