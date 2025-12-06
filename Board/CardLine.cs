using Godot;

public partial class CardLine : PanelContainer
{
    protected HBoxNodeContainer Container = null!;
    protected IChildManagerComponent ContainerNodeManager => Container;    

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
        Render();
    }

    protected void Render()
    {
        if(!IsInsideTree()) return;
        RenderCore();
    }

    protected virtual void RenderCore()
    {
        if(_shrinks)
        {
            ResetSize();
        }
    }

    public virtual void AddCard(Card card) {}
}
