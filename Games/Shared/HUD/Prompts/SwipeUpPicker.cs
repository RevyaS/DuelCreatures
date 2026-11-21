using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// Picking one: "use this skill?", which skill resolves first, which option of a skill. The choices are cards in the
/// %Row (a SelectionGrid); swiping one up out of the row, onto the %PickArea above it (a DropArea), picks it; let go
/// anywhere else, it goes back. %PickHint shows while a card is over the pick area. A tap on a card shows what it stands
/// for in the %DetailsSpace. Only %Buttons' Cancel declines (shown when the question may be declined): a press elsewhere
/// never does. Above, the asking skill (a ResolvingSkillDetails banner, if the layout has one) and the %Header.
/// </summary>
public partial class SwipeUpPicker : InputScreen
{
    PromptHeader header = null!;
    SelectionGrid row = null!;
    PromptButtons buttons = null!;
    DropArea pickArea = null!;
    Control pickHint = null!;

    public override void _Ready()
    {
        base._Ready();
        header = GetNode<PromptHeader>("%Header");
        row = GetNode<SelectionGrid>("%Row");
        buttons = GetNode<PromptButtons>("%Buttons");
        pickArea = GetNode<DropArea>("%PickArea");
        pickHint = GetNode<Control>("%PickHint");
        pickHint.Hide();
        pickArea.DragHoverChanged += hovering => pickHint.Visible = hovering;
    }

    /// <summary>
    /// The option swiped up, or (false, default) when declined with Cancel (<paramref name="canCancel"/>).
    /// <paramref name="showDetails"/> shows what a tapped option stands for.
    /// </summary>
    public async Task<(bool Picked, T Item)> PickOne<T>(string hint, IReadOnlyList<SwipeUpOption<T>> options, bool canCancel, Action<T> showDetails)
    {
        if(options.Count == 0) throw new ArgumentException($"{GetPath()}: nothing to pick from for \"{hint}\"", nameof(options));
        Open(hint);
        var items = row.Show(options.Select(option => option.Card));
        var picked = new TaskCompletionSource<int>();
        int IndexOf(Func<SelectableCard, bool> match) => items.Select((item, index) => (item, index)).First(entry => match(entry.item)).index;
        void OnPressed(SelectableCard pressed) => showDetails(options[IndexOf(item => ReferenceEquals(item, pressed))].Item);
        void OnDropped(Card node)
        {
            if(items.Any(item => item.Shows(node))) picked.TrySetResult(IndexOf(item => item.Shows(node)));
        }
        try
        {
            header.Hint = hint;
            header.HideCount();
            buttons.ShowsOk = false;
            buttons.CanCancel = canCancel;
            for(var i = 0; i < items.Count; i++)
            {
                items[i].Tag = options[i].Tag;
                items[i].Draggable = true;
            }
            row.CardPressed += OnPressed;
            pickArea.CardDropped += OnDropped;

            while(true)
            {
                var cancel = buttons.NextPress();
                var first = await Task.WhenAny(picked.Task, cancel);
                if(first == picked.Task) return (true, options[await picked.Task].Item);
                if(await cancel == PromptButton.Cancel) return (false, default!);
            }
        }
        finally
        {
            row.CardPressed -= OnPressed;
            pickArea.CardDropped -= OnDropped;
            pickHint.Hide();
            row.Clear();
            Close();
        }
    }
}
