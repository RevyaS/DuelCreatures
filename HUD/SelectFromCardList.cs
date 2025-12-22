using System;
using System.Collections.Generic;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

public partial class SelectFromCardList : VBoxContainer
{
    SelectCardsComponent SelectCardsComponent = null!;
    CardListPanel CardListPanel = null!;

    public override void _Ready()
    {
        SelectCardsComponent = GetNode<SelectCardsComponent>($"%{nameof(SelectCardsComponent)}");
        CardListPanel = GetNode<CardListPanel>($"%{nameof(CardListPanel)}");

        SelectCardsComponent.CardSelected += OnCardSelected;
        SelectCardsComponent.CardDragging += OnSelectedCardDragging;
        SelectCardsComponent.ConfirmedCards += OnConfirmedCards;
        CardListPanel.CardDragging += OnCardDragging;
        CardListPanel.CardDropped += OnCardDropped;
    }

    private void OnConfirmedCards(List<Card> list)
    {
        ConfirmedCards?.Invoke(list);
    }

    public void Show(string cardListTitle, string selectionTitle, int minSelection, int maxSelection, List<VanguardCard> list)
    {
        CardListPanel.Setup(cardListTitle, list);
        SelectCardsComponent.Activate(selectionTitle, minSelection, maxSelection);
        SelectCardsComponent.Droppable = false;
        CardListPanel.Droppable = false;
        CardListPanel.CardsDraggable = true;
        Show();
    }

    public void Deactivate()
    {
        SelectCardsComponent.Deactivate();
        Hide();
    }

    private void OnCardDropped(Card card)
    {
        SelectCardsComponent.RemoveCard(card);
        CardListPanel.AddCard(card);
        card.CurrentlyDragged = false;
        CardListPanel.Droppable = false;
    }

    private void OnSelectedCardDragging(CardBaseComponent component)
    {
        CardListPanel.Droppable = true;
    }

    private void OnCardDragging(CardBaseComponent component)
    {
        SelectCardsComponent.Droppable = true;
    }

    private void OnCardSelected(Card card)
    {
        CardListPanel.RemoveCard(card);
        SelectCardsComponent.Droppable = false;
    }


    public event Action<List<Card>>? ConfirmedCards;
}