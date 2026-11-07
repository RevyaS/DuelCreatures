using System.Collections.Generic;
using Godot;

/// <summary>
/// One button per command the IActivationGate an ancestor provides offers for the card where it is (e.g. "Summon" on
/// an Xyz monster in the Extra Deck). Rebuilt whenever the gate changes, so only what can be done now is offered.
/// Hidden when there is none, when read-only (no gate) and when one effect is in focus.
/// </summary>
[Tool]
public partial class CardCommandsPart : HBoxContainer, ICardDetailPart
{
    [Export] public int FontSize { get; set; } = 32;

    CardDetail? detail;
    IActivationGate? gate;

    public void Show(CardDetail detail)
    {
        this.detail = detail;
        UseGate(detail.IsFocused ? null : ActivationGates.ProvidedTo(this));
        Refresh();
    }

    private void Refresh()
    {
        this.FreeChildren();
        IReadOnlyList<CardCommand> commands = detail is not null && gate is not null ? gate.CommandsFor(detail.Card, detail.From) : [];
        foreach(var command in commands)
        {
            var button = new Button { Text = command.Label, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            button.AddThemeFontSizeOverride("font_size", FontSize);
            button.Pressed += () =>
            {
                if(detail is not null && gate is not null) gate.Invoke(detail.Card, detail.From, command);
            };
            AddChild(button);
        }
        Visible = commands.Count > 0;
    }

    private void UseGate(IActivationGate? next)
    {
        if(ReferenceEquals(gate, next)) return;
        if(gate is not null) gate.Changed -= Refresh;
        gate = next;
        if(gate is not null) gate.Changed += Refresh;
    }

    public override void _ExitTree()
    {
        if(gate is not null) gate.Changed -= Refresh;
        gate = null;
    }
}
