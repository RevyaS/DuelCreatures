using ArC.CardGames.Components;
using DuelCreatures.Data;
using Godot;

// A pile showing its top card (deck, drop zone), sized by CardScale
[Tool]
public partial class CardVerticalStack : PanelContainer, ICardLocation
{
    CardContainer cardContainer = null!;
    PressArea PressArea = null!;
    Label CardCount = null!;

    private float _cardScale = SizeConstants.DefaultCardScale;
    [Export(PropertyHint.Range, "0.05, 2.0, 0.01, or_greater")]
    public float CardScale
    {
        get => _cardScale;
        set
        {
            _cardScale = value;
            Render();
        }
    }

    private int _countValue = 0;
    public int CountValue
    {
        get => _countValue;
        set
        {
            _countValue = value;
            Render();
        }
    }

    private bool _showCount = false;
    [Export]
    public bool ShowCount
    {
        get => _showCount;
        set
        {
            _showCount = value;
            Render();
        }
    }

    private SleeveInfo _sleeveInfo = null!;
    [Export]
    public SleeveInfo SleeveInfo
    {
        get => _sleeveInfo;
        set
        {
            _sleeveInfo = value;
            Render();
        }
    }

    protected virtual void BeforeRender() {}

    protected void Render()
    {
        if(!IsInsideTree() || !IsNodeReady()) return;
        BeforeRender();
        CardCount.Visible = ShowCount;
        CardCount.Text = CountValue.ToString();

        cardContainer.CardScale = CardScale;
        cardContainer.SetSleeveInfo(SleeveInfo);
    }

    public override void _Ready()
    {
        CardCount = GetNode<Label>($"%{nameof(CardCount)}");
        cardContainer = GetNode<CardContainer>($"%{nameof(CardContainer)}");
        PressArea = GetNode<PressArea>($"%{nameof(PressArea)}");
        PressArea.Pressed += OnPressed;
        Render();
    }

    public Card? FindCardNode(CardBase card)
    {
        var shown = cardContainer.CurrentCard;
        return shown is not null && ReferenceEquals(shown.CurrentCard, card) ? shown : null;
    }

    public Transform2D GetCardTransform(CardBase? card)
    {
        return cardContainer.GetCardGlobalTransform();
    }

    protected virtual void OnPressed()
    {
    }

    public void ClearCard()
    {
        cardContainer.RemoveCardAndFree();
    }
    public void SetCard(CardBase card)
    {
        var cardComponent = CardScenes.Create(card);
        cardContainer.AddCard(cardComponent);
    }
}
