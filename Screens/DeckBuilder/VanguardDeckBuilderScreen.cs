using System.Collections.Generic;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

public partial class VanguardDeckBuilderScreen : VBoxContainer
{
    DataManager _dataManager;

    GridContainer CardPoolList;
    CardArea StarterContainer;
    public override void _Ready()
    {
        _dataManager = DataManager.Get(this);

        StarterContainer = GetNode<CardArea>($"%{nameof(StarterContainer)}");
        CardPoolList = GetNode<GridContainer>($"%{nameof(CardPoolList)}");

        string test = "Oracle Think Tank";
        LoadCards(test);
    }

    public void LoadDeck(DeckInfo deckInfo)
    {
        // Add Starter

    }

    private void LoadCards(string cards)
    {
        CardPoolList.ClearChildren();
        foreach(var card in _dataManager.Cards.Cards)
        {

        }
    }

    public void Init(List<VanguardCard> decklist)
    {

    }
}
