using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Components;
using ArC.CardGames.Rules;
using Godot;

/// <summary>
/// The skill of the player's that is being activated or resolved, pinned over the board while it asks them things (its
/// targets, its costs, its options): the game's card details scene under it, showing only that skill. It can't be
/// closed; it goes when the skill is done. The ordinary card details stay free to inspect the options. A skill used
/// while another one resolves is shown on top, then the outer one again. Put the game's details scene under it, in a
/// PromptLayer (swiping down on it collapses the layer).
/// </summary>
public partial class ResolvingSkillDetails : Control, ICollapsiblePrompt
{
    CardDetailsView details = null!;
    readonly List<(CardBase Card, ISkill Skill)> resolving = [];

    // The details fill the panel and take its presses: swiping on them collapses the layer
    public Control SwipeArea => details;
    public bool IsShowing => IsVisibleInTree();

    public override void _Ready()
    {
        details = this.Descendants<CardDetailsView>().FirstOrDefault()
            ?? throw new System.InvalidOperationException($"{GetPath()}: a {nameof(ResolvingSkillDetails)} holds the game's card details scene");
        Hide();
    }

    /// <summary><paramref name="skill"/> of <paramref name="card"/> started asking the player things: it's shown until <see cref="Finished"/>.</summary>
    public void Started(CardBase card, ISkill skill)
    {
        resolving.Add((card, skill));
        ShowLatest();
    }

    /// <summary><paramref name="skill"/> is done: the skill around it (if any) shows again, or nothing.</summary>
    public void Finished(ISkill skill)
    {
        var index = resolving.FindLastIndex(entry => ReferenceEquals(entry.Skill, skill));
        if(index >= 0) resolving.RemoveAt(index);
        ShowLatest();
    }

    private void ShowLatest()
    {
        if(resolving.Count == 0)
        {
            Hide();
            return;
        }
        var (card, skill) = resolving[^1];
        details.Show(new CardDetail(card) { Focus = skill });
        Show();
    }
}
