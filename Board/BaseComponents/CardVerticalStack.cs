using ArC.CardGames.Predefined.Vanguard;
using DuelCreatures.Data;
using Godot;

public partial class CardVerticalStack : PanelContainer
{
    CardContainer cardContainer = null!;
    PressArea PressArea = null!;
    Label CardCount = null!;

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
        if(!IsInsideTree()) return;
        BeforeRender();
        CardCount.Visible = ShowCount;
        CardCount.Text = CountValue.ToString();

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

    protected virtual void OnPressed()
    {
    }

    public void ClearCard()
    {
        cardContainer.RemoveCardAndFree();
    }
    public void SetCard(VanguardCard card)
    {
        var cardComponent = SceneFactory.CreateVanguardCard(card);
        cardContainer.AddCard(cardComponent);
    }
}
