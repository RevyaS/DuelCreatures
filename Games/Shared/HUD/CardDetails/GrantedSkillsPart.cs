using System.Linq;
using ArC.CardGames.Rules;
using Godot;

/// <summary>Skills effects gave the card (e.g. "this unit gets '[AUTO] ...' until end of turn"); hidden when there are none.</summary>
[Tool]
public partial class GrantedSkillsPart : RichTextLabel, ICardDetailPart
{
    [Export] public string Title { get; set; } = "Granted:";
    [Export] public Color TextColor { get; set; } = new(0.55f, 0.85f, 1f);

    public void Show(CardDetail detail)
    {
        Clear();
        Visible = detail.Focus is null && detail.GrantedSkills.Count > 0;
        if(!Visible) return;

        var interpreter = this.DependOn<ISkillTextInterpreter>(SkillKindInterpreter.Instance);
        PushColor(TextColor);
        AddText(Title);
        Newline();
        AddText(string.Join("\n", detail.GrantedSkills.Select(interpreter.Describe)));
        Pop();
    }
}
