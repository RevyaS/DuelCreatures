using System.Linq;
using Godot;

/// <summary>
/// The skill of the player's that is being activated or resolved (<see cref="ResolvingSkills"/>), shown while it asks
/// them things (its targets, its costs, its options): the game's card details scene under it, showing only that skill.
/// It can't be closed; it goes when the skill is done. A skill used while another one resolves is shown on top, then
/// the outer one again. Pinned over the board in a PromptLayer (swiping down on it collapses the layer), it steps aside
/// while an input screen is up; inside an input screen it is that screen's skill banner.
/// </summary>
public partial class ResolvingSkillDetails : Control, ICollapsiblePrompt
{
    CardDetailsView details = null!;
    ResolvingSkills? source;

    // The details fill the panel and take its presses: swiping on them collapses the layer
    public Control SwipeArea => details;
    public bool IsShowing => IsVisibleInTree();

    public override void _Ready()
    {
        details = this.Descendants<CardDetailsView>().FirstOrDefault()
            ?? throw new System.InvalidOperationException($"{GetPath()}: a {nameof(ResolvingSkillDetails)} holds the game's card details scene");
        Refresh();
    }

    public override void _EnterTree() => InputScreen.CurrentChanged += Refresh;

    public override void _ExitTree()
    {
        InputScreen.CurrentChanged -= Refresh;
        if(source is not null) source.Changed -= Refresh;
    }

    /// <summary>Shows the skills <paramref name="skills"/> tracks, as they start and finish.</summary>
    public void Follow(ResolvingSkills skills)
    {
        if(source is not null) source.Changed -= Refresh;
        source = skills;
        skills.Changed += Refresh;
        Refresh();
    }

    // An input screen is up, and this isn't its banner
    bool SteppedAside => InputScreen.Current is { } screen && !screen.IsAncestorOf(this);

    private void Refresh()
    {
        if(!IsNodeReady()) return;
        if(source?.Current is not { } current || SteppedAside)
        {
            Hide();
            return;
        }
        details.Show(new CardDetail(current.Card) { Focus = current.Skill });
        Show();
    }
}
