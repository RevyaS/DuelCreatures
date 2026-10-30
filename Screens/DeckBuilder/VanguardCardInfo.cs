using System;
using ArC.CardGames.Vanguard;
using Godot;

/// <summary>A card shown big, with how many copies are owned and in the deck, and its details.</summary>
public partial class VanguardCardInfo : VBoxContainer
{
    const float CardScale = 2.8f;

    Label Owned = null!;
    Control CardSlot = null!;
    CardDetails CardDetails = null!;
    Button BackButton = null!;

    public event Action? Closed;

    public override void _Ready()
    {
        Owned = GetNode<Label>($"%{nameof(Owned)}");
        CardSlot = GetNode<Control>($"%{nameof(CardSlot)}");
        CardDetails = GetNode<CardDetails>($"%{nameof(CardDetails)}");
        BackButton = GetNode<Button>($"%{nameof(BackButton)}");
        BackButton.Pressed += () => Closed?.Invoke();
    }

    public void ShowCard(string cardId, VanguardCard card, int owned, int inDeck)
    {
        CardSlot.FreeChildren();
        CardSlot.AddChild(CardView.Create(cardId, card, CardScale));

        Owned.Text = $"Owned:{owned}      In Deck:{inDeck}";
        CardDetails.ShowCard(card);
    }
}
