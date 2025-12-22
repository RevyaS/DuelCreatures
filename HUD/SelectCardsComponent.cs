using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class SelectCardsComponent : PanelContainer
{
    int minCards = 0, maxCards = 0;

    Label Title = null!;
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
        
        int selectedCount = SelectedCards.CardCount;
        Confirm.Disabled = selectedCount < minCards || selectedCount > maxCards;
        DropArea.Visible = Droppable;
    }

    public override void _Ready()
    {
        Title = GetNode<Label>($"%{nameof(Title)}");
        DropArea = GetNode<DropArea>($"%{nameof(DropArea)}");
        DropArea.CardDropped += OnCardDropped;

        SelectedCards = GetNode<CardLineDynamic>($"%{nameof(SelectedCards)}");
        SelectedCards.CardDragging += OnCardDragging;
        
        Confirm = GetNode<Button>($"%{nameof(Confirm)}");

        Confirm.Pressed += OnConfirm;

        Render();
    }

    private void OnCardDragging(CardBaseComponent component)
    {
        CardDragging?.Invoke(component);
    }

    private void OnConfirm()
    {
        ConfirmedCards?.Invoke(SelectedCards.GetCards().ToList());
    }

    public void Activate(string title, int minCards, int maxCards)
    {
        this.minCards = minCards;
        this.maxCards = maxCards;
        Title.Text = title;
        SelectedCards.ClearCards();
        Render();
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
        Render();
    }
    
    public void RemoveCard(Card card)
    {
        SelectedCards.RemoveCard(card);
        Render();
    }

    public override void _Notification(int what)
    {
        if(what == NotificationDragEnd)
        {
            var containedCard = SelectedCards.FindCard(selected => selected.CurrentlyDragged);

            if(containedCard is not null)
            {
                RemoveCard(containedCard);

                if(!IsDragSuccessful())
                {
                    CardReturned?.Invoke(containedCard);
                }
            }
        }
    }

    public event Action<Card>? CardReturned;
    public event Action<Card>? CardSelected;
    public event Action<CardBaseComponent>? CardDragging;
    public event Action<List<Card>>? ConfirmedCards;
}
