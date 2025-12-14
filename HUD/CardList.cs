using System.Collections.Generic;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

public partial class CardList : Control
{
    Label Title = null!, Amount = null!;
    HFlowContainer CardContainerList = null!;

    public override void _Ready()
    {
        Title = GetNode<Label>($"%{nameof(Title)}");
        Amount = GetNode<Label>($"%{nameof(Amount)}");
        CardContainerList = GetNode<HFlowContainer>($"%{nameof(CardContainerList)}");
    }

    public override void _GuiInput(InputEvent e)
    {
        // Mouse click or touch
        if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            Hide();
        }

        if (e is InputEventScreenTouch st && st.Pressed)
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
            CardContainerList.AddChild(container);
        }

        Show();
    }
}