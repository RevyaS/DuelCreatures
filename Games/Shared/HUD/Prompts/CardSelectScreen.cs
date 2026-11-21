using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ArC.CardGames.Components;

/// <summary>
/// Card selection: the offered cards, enlarged, in the %Grid (a SelectionGrid), each with where it is (its location
/// tag) and, for a counter blast or charge, how it will face. A tap on a card shows its details in the %DetailsSpace;
/// its select button picks it (numbered in picking order), again drops it; select buttons on unpicked cards are off once
/// the most are picked. OK answers once the count fits; Cancel, shown when the question may be declined, answers
/// nothing. Above, the asking skill (a ResolvingSkillDetails banner, if the layout has one) and the %Header.
/// </summary>
public partial class CardSelectScreen : InputScreen
{
    PromptHeader header = null!;
    SelectionGrid grid = null!;
    PromptButtons buttons = null!;

    /// <summary>A card was tapped: show its details.</summary>
    public event Action<CardBase>? CardPressed;

    public override void _Ready()
    {
        base._Ready();
        header = GetNode<PromptHeader>("%Header");
        grid = GetNode<SelectionGrid>("%Grid");
        buttons = GetNode<PromptButtons>("%Buttons");
        grid.CardPressed += card => CardPressed?.Invoke(card.Card);
    }

    /// <summary>The picks answering <paramref name="request"/>, in picking order; none when cancelled.</summary>
    public async System.Threading.Tasks.Task<IReadOnlyList<CardBase>> ChooseCards(CardSelectRequest request)
    {
        Open(request.Hint);
        var selection = new CardSelection<CardBase>(request.Cards, request.Minimum, request.Maximum);
        var items = grid.Show(selection, card => card);
        void ShowFacing()
        {
            foreach(var item in items) item.Facing = request.FacingOf?.Invoke(item.Card, selection.IsPicked(item.Card)) ?? FacingMark.None;
        }
        try
        {
            header.Hint = request.Hint;
            header.Follow(selection);
            buttons.ShowsOk = true;
            buttons.CanCancel = request.CanCancel;
            buttons.Follow(selection);
            foreach(var item in items) item.Tag = request.TagOf(item.Card);
            selection.Changed += ShowFacing;
            ShowFacing();

            while(true)
            {
                switch(await buttons.NextPress())
                {
                    case PromptButton.Ok when selection.CanConfirm:
                        return selection.Picks.ToList();
                    case PromptButton.Cancel:
                        return [];
                }
            }
        }
        finally
        {
            selection.Changed -= ShowFacing;
            header.HideCount();
            buttons.Unfollow();
            grid.Clear();
            Close();
        }
    }
}
