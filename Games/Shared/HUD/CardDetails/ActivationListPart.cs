using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Rules;
using Godot;

/// <summary>
/// One button per skill the player activates (IManuallyActivatedSkill), own and granted. A button is enabled only while
/// the IActivationGate an ancestor provides allows it, and is re-checked whenever the gate changes, so it can't be
/// pressed when the game isn't asking. With no gate provided (read-only, e.g. the deck builder) it shows nothing.
/// </summary>
[Tool]
public partial class ActivationListPart : VBoxContainer, ICardDetailPart
{
    [Export] public int FontSize { get; set; } = 32;

    readonly List<(Button Button, ISkill Skill)> buttons = [];
    CardDetail? detail;
    IActivationGate? gate;

    public void Show(CardDetail detail)
    {
        this.detail = detail;
        this.FreeChildren();
        buttons.Clear();

        // Read-only (no gate provided, e.g. the deck builder) or one effect in focus: no buttons, the skill text shows it
        if(detail.Focus is not null || ActivationGates.ProvidedTo(this) is not { } provided)
        {
            UseGate(null);
            Visible = false;
            return;
        }
        UseGate(provided);
        var interpreter = this.DependOn<ISkillTextInterpreter>(SkillKindInterpreter.Instance);
        foreach(var skill in detail.AllSkills.OfType<IManuallyActivatedSkill>())
        {
            var button = new Button
            {
                Text = interpreter.Describe(skill),
                ClipText = true,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                Alignment = HorizontalAlignment.Left,
            };
            button.AddThemeFontSizeOverride("font_size", FontSize);
            button.Pressed += () => OnPressed(skill);
            AddChild(button);
            buttons.Add((button, skill));
        }
        Visible = buttons.Count > 0;
        Refresh();
    }

    private void OnPressed(ISkill skill)
    {
        if(detail is null || gate is null || !gate.CanActivate(detail.Card, detail.From, skill)) return;
        gate.Activate(detail.Card, detail.From, skill);
    }

    // Enabled exactly while the gate allows it
    private void Refresh()
    {
        foreach(var (button, skill) in buttons)
        {
            button.Disabled = detail is null || gate is null || !gate.CanActivate(detail.Card, detail.From, skill);
        }
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
