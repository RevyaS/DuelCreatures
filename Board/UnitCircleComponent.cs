using System;
using System.Threading.Tasks;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

[Tool]
public partial class UnitCircleComponent : Control, IEventBusUtilizer
{
    CardRotationContainer cardRotationContainer = null!;
    DropArea dropArea = null!;

    public UnitCircle UnitCircle { get; private set; } = null!;

    private bool _droppable = false;
    [Export]
    public bool Droppable
    {
        get => _droppable;
        set
        {
            _droppable = value;
            Render();
        }
    }

    private bool _draggable = false;
    [Export]
    public bool Draggable
    {
        get => _draggable;
        set
        {
            _draggable = value;
            Render();
        }
    }

    public override void _Ready()
    {
        cardRotationContainer = GetNode<CardRotationContainer>($"%{nameof(CardRotationContainer)}");
        dropArea = GetNode<DropArea>($"%{nameof(DropArea)}");
        dropArea.CardDropped += OnCardDropped;
        Render();
    }

    public void SetEventBus(VanguardEventBus eventBus)
    {
        eventBus.OnPlaced += OnPlacedHandler;
    }

    private Task OnPlacedHandler(UnitCircle circle)
    {
        if(ReferenceEquals(circle, UnitCircle))
        {
            // Assign card
            SetCard(circle.Card!);
        }
        return Task.CompletedTask;
    }

    public void BindUnitCircle(UnitCircle unitCircle)
    {
        UnitCircle = unitCircle;
    }

    private void Render()
    {
        if(!IsInsideTree()) return;
        dropArea.Visible = _droppable;
        cardRotationContainer.Draggable = _draggable;
    }

    protected virtual void OnCardDropped(Card card)
    {
        CardDropped?.Invoke(card);
    }

    public void ClearCard()
    {
        cardRotationContainer.RemoveCard();
    }

    public void FaceUp()
    {
        cardRotationContainer.FaceUp();
    }

    public void SetCard(VanguardCard card)
    {
        Card cardComponent = SceneFactory.CreateVanguardCard(card);
        cardRotationContainer.AddCard(cardComponent);
        cardRotationContainer.FaceUp();
    }
    public event Action<Card>? CardDropped;
}
