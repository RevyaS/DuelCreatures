using System;
using System.Collections.Generic;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

public partial class CardList : Control
{
    Label Title = null!, Amount = null!;
    HFlowNodeContainer CardContainerList = null!;
    IChildManagerComponent CardContainerManager = null!;

    private bool _cardDraggable = false;
    public bool CardsDraggable { 
        get => _cardDraggable;
        set
        {
            _cardDraggable = value;
            Render();
        }
    }

    public bool BaseDroppable { get; set; }

    public override void _Ready()
    {
        Title = GetNode<Label>($"%{nameof(Title)}");
        Amount = GetNode<Label>($"%{nameof(Amount)}");
        CardContainerList = GetNode<HFlowNodeContainer>($"%{nameof(CardContainerList)}");
        CardContainerManager = CardContainerList;
    }

    private void Render()
    {
        CardContainerManager.ApplyToChildren<CardContainer>(container =>
        {
            container.Draggable = CardsDraggable;
        });
    }

    public override void _GuiInput(InputEvent e)
    {
        // Mouse click or touch
        if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            Hide();
        }
    }

    public void Show(string title, List<VanguardCard> cardsToShow)
    {
        Title.Text = title;
        Amount.Text = $"({cardsToShow.Count})";
        CardContainerList.ClearChildren();
        foreach(var card in cardsToShow)
        {
            CardContainer container = new();
            container.AddCard(SceneFactory.CreateVanguardCard(card));
            container.CardPressed += OnCardPressed;
            CardContainerList.AddChild(container);
        }

        Show();
    }

    private void OnCardPressed(Card card)
    {
        CardPressed?.Invoke(card);
    }

    public event Action<Card>? CardPressed;
}