using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

public partial class VanguardCardComponent : Card
{
    Label PowerLabel = null!, CriticalLabel = null!, GradeLabel = null!;
    PanelContainer PowerPanel = null!, GradePanel = null!;
    public override VanguardCard CurrentCard => (VanguardCard)base.CurrentCard;

    
    private int _power = 0;
    public int Power
    {
        get => _power;
        set
        {
            _power = value;
            Render();
        }
    }

    private int _critical = 0;
    public int Critical
    {
        get => _critical;
        set
        {
            _critical = value;
            Render();
        }
    }

    private int _grade = 0;
    public int Grade
    {
        get => _grade;
        set
        {
            _grade = value;
            Render();
        }
    }
    
    
    public override void _Ready()
    {
        PowerLabel = GetNode<Label>($"%{nameof(PowerLabel)}");
        CriticalLabel = GetNode<Label>($"%{nameof(CriticalLabel)}");
        GradeLabel = GetNode<Label>($"%{nameof(GradeLabel)}");
        PowerPanel = GetNode<PanelContainer>($"%{nameof(PowerPanel)}");
        GradePanel = GetNode<PanelContainer>($"%{nameof(GradePanel)}");
        base._Ready();
    }

    protected override void RenderCore()
    {
        base.RenderCore();
        PowerLabel.Text = Power.ToString();
        CriticalLabel.Text = Critical.ToString();
        GradeLabel.Text = Grade.ToString();

        PowerPanel.Visible = IsFront;
        GradePanel.Visible = IsFront;
    }

    public override void LoadVanguardCard(CardBase card)
    {
        VanguardCard vgcard = (VanguardCard)card;
        Power = vgcard.Power;
        Critical = vgcard.Critical;
        Grade = vgcard.Grade;

        base.LoadVanguardCard(card);
    }

    public override Card CreateClone()
    {
        var card = SceneFactory.CreateVanguardCard(CurrentCard);
        return card;
    }
}
