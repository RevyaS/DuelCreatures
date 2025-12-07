using Godot;

public partial class CardLine : PanelContainer
{
    protected HBoxNodeContainer Container = null!;
    protected IChildManagerComponent ContainerNodeManager => Container;    

    private float _cardScale = SizeConstants.CardScaleFactor;
    [Export(PropertyHint.Range, "0.0, 1.0")]
    public float CardScale
    {
        get => _cardScale;
        set
        {
            _cardScale = value;
            Render();
        }
    }

    private int _separation = 0;
    [Export]
    public int Separation { 
        get => _separation; 
        set
        {
            _separation = value;
            Render();
        } 
    }

    private bool _shrinks = false;
    [Export]
    public bool Shrinks
    {
        get => _shrinks;
        set
        {
            _shrinks = value;
            Render();
        }
    }

    public override void _Ready()
    {
        Container = GetNode<HBoxNodeContainer>($"%{nameof(Container)}");
        OnComponentsSet();   
        Render();
    }

    protected virtual void OnComponentsSet()
    {
    }

    protected void Render()
    {
        if(!IsInsideTree()) return;
        RenderCore();
    }

    protected virtual void RenderCore()
    {
        Container.RemoveThemeConstantOverride("separation");
        Container.AddThemeConstantOverride("separation", _separation);

        ContainerNodeManager.ApplyToChildren<CardContainer>(container =>
        {
            container.CardScale = CardScale;
        });

        if(_shrinks)
        {
            ResetSize();
        }
    }

    public virtual void AddCard(Card card) {}
}
