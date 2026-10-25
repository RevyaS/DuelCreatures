using DuelCreatures.Data;
using Godot;

/// <summary>A pulled card in the opening's arc: the card (face down until revealed) over a glow of the rarity it shows.</summary>
public partial class GachaCardSlot : Control
{
    const float AuraSize = 1.6f;

    public PackCard PackCard { get; private set; } = null!;
    public CardView View { get; private set; } = null!;
    public RarityAura Aura { get; private set; } = null!;
    public bool Revealed { get; set; }

    /// <summary>Where the card rests in the arc.</summary>
    public Vector2 RestPosition { get; set; }
    public float RestRotation { get; set; }

    public static GachaCardSlot Create(PackCard packCard, CardView view, SleeveInfo sleeve)
    {
        var slot = new GachaCardSlot();
        slot.Initialize(packCard, view, sleeve);
        return slot;
    }

    private void Initialize(PackCard packCard, CardView view, SleeveInfo sleeve)
    {
        PackCard = packCard;
        View = view;
        Size = view.CustomMinimumSize;
        PivotOffset = Size / 2;
        MouseFilter = MouseFilterEnum.Ignore;

        Aura = new RarityAura { Size = Size * AuraSize, Position = Size * (1 - AuraSize) / 2 };
        ShowRarity(packCard.ShownRarity);
        AddChild(Aura);

        View.SleeveInfo = sleeve;
        View.IsFront = false;
        AddChild(View);
    }

    // Commons don't glow, so the hits stand out
    public void ShowRarity(Rarity rarity)
    {
        Aura.SetRarity(rarity);
        Aura.Intensity = rarity.Tier == 0 ? 0 : 0.5f + 0.5f * rarity.Tier / GachaConfig.Rarities[^1].Tier;
    }
}
