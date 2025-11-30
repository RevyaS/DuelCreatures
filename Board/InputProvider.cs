using Godot;

public partial class InputProvider : Control
{
    DuelCreaturesBoard board;
    public DuelCreaturesBoard Board => board;

    #region States
    bool DragHandToZone = false;
    #endregion


    SelectCardsFromHandComponent SelectCardsFromHandComponent;

    public override void _Ready()
    {
        board = GetNode<DuelCreaturesBoard>($"%{nameof(DuelCreaturesBoard)}");
        SelectCardsFromHandComponent = GetNode<SelectCardsFromHandComponent>($"%{nameof(SelectCardsFromHand)}");
        Board.HandCardPressed += OnHandCardPressed;
    }

    private void OnHandCardPressed(Card card)
    {
        GD.Print();
    }

    public void SelectCardsFromHand()
    {
        DragHandToZone = true;
        SelectCardsFromHandComponent.Show();
        
    }
}
