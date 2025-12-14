using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

public partial class VanguardCardComponent : Card
{
    Label PowerLabel = null!, CriticalLabel = null!, GradeLabel = null!, GuardLabel = null!;
    PanelContainer PowerPanel = null!, GradePanel = null!, GuardPanel = null!;
    public override VanguardCard CurrentCard => (VanguardCard)base.CurrentCard;

    private bool _guardMode = false;
    public bool GuardMode
    {
        get => _guardMode;
        set
        {
            _guardMode = value;
            Render();
        }
    }
    
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

    private int _guard = 0;
    public int Guard
    {
        get => _guard;
        set
        {
            _guard = value;
            Render();
        }
    }
    
    public override void _Ready()
    {
        PowerLabel = GetNode<Label>($"%{nameof(PowerLabel)}");
        CriticalLabel = GetNode<Label>($"%{nameof(CriticalLabel)}");
        GradeLabel = GetNode<Label>($"%{nameof(GradeLabel)}");
        GuardLabel = GetNode<Label>($"%{nameof(GuardLabel)}");
        PowerPanel = GetNode<PanelContainer>($"%{nameof(PowerPanel)}");
        GradePanel = GetNode<PanelContainer>($"%{nameof(GradePanel)}");
        GuardPanel = GetNode<PanelContainer>($"%{nameof(GuardPanel)}");
        base._Ready();
    }

    protected override void RenderCore()
    {
        base.RenderCore();
        PowerLabel.Text = Power.ToString();
        CriticalLabel.Text = Critical.ToString();
        GradeLabel.Text = Grade.ToString();
        GuardLabel.Text = Guard.ToString();

        PowerPanel.Visible = IsFront && !GuardMode;
        GradePanel.Visible = IsFront;
        GuardPanel.Visible = IsFront && GuardMode;
    }

    public override void LoadVanguardCard(CardBase card)
    {
        VanguardCard vgcard = (VanguardCard)card;
        Power = vgcard.Power;
        Critical = vgcard.Critical;
        Grade = vgcard.Grade;
        Guard = vgcard.Guard;

        base.LoadVanguardCard(card);
    }

    public override Card CreateClone()
    {
        var card = SceneFactory.CreateVanguardCard(CurrentCard);
        card.GuardMode = GuardMode;
        return card;
    }
}
