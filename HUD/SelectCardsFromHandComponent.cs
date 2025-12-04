using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class SelectCardsFromHandComponent : PanelContainer
{
    int minCards = 0, maxCards = 0;

    DropArea DropArea = null!;
    CardLineDynamic SelectedCards = null!;
    Button Confirm = null!;

    private bool _droppable;
    public bool Droppable
    {
        get => _droppable;
        set
        {
            _droppable = value;
            Render();
        }
    }

    private void Render()
    {
        if(!IsInsideTree()) return;
        
        DropArea.Visible = Droppable;
    }

    public override void _Ready()
    {
        DropArea = GetNode<DropArea>($"%{nameof(DropArea)}");
        DropArea.CardDropped += OnCardDropped;

        SelectedCards = GetNode<CardLineDynamic>($"%{nameof(SelectedCards)}");
        Confirm = GetNode<Button>($"%{nameof(Confirm)}");

        Confirm.Pressed += OnConfirm;

        Render();
    }

    private void OnConfirm()
    {
        ConfirmedCards?.Invoke(SelectedCards.GetCards().ToList());
    }

    public void Activate(int minCards, int maxCards)
    {
        SelectedCards.ClearCards();
        Show();
    }

    public void Deactivate()
    {
        SelectedCards.ClearCards();
        Hide();
    }

    private void OnCardDropped(Card card)
    {
        CardSelected?.Invoke(card);
        SelectedCards.AddCard(card);
        card.CurrentlyDragged = false;
    }

    public override void _Notification(int what)
    {
        if(what == NotificationDragEnd)
        {
            var containedCard = SelectedCards.FindCard(selected => selected.CurrentlyDragged);

            if(containedCard is not null)
            {
                SelectedCards.RemoveCard(containedCard);

                if(!IsDragSuccessful())
                {
                    CardReturned?.Invoke(containedCard);
                }
            }
        }
    }

    public event Action<Card>? CardReturned;
    public event Action<Card>? CardSelected;
    public event Action<List<Card>>? ConfirmedCards;
}
