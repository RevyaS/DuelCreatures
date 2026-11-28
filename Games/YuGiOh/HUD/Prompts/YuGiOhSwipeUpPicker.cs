using System;
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Skills;
using Godot;

/// <summary>
/// YuGiOh's swipe-up picker: the shared one (effects, options, "activate?"), plus the chain being responded to, shown
/// above the prompt in %ChainSection's %Chain (a SelectionGrid, only to read): a tap on a link shows the effect it
/// activated, without answering.
/// </summary>
public partial class YuGiOhSwipeUpPicker : SwipeUpPicker
{
    Control chainSection = null!;
    SelectionGrid chain = null!;

    public override void _Ready()
    {
        base._Ready();
        chainSection = GetNode<Control>("%ChainSection");
        chain = GetNode<SelectionGrid>("%Chain");
        chainSection.Hide();
    }

    /// <summary>
    /// Shows <paramref name="links"/> (first to last) until the result is disposed; a tapped link goes to
    /// <paramref name="showLink"/>. No links: nothing shows.
    /// </summary>
    public IDisposable ShowChain(IReadOnlyList<ChainLink> links, Action<ChainLink> showLink)
    {
        if(links.Count == 0) return new Disposable(() => {});
        var items = chain.Show(links.Select(link => link.Activation.Card));
        void OnPressed(SelectableCard pressed)
        {
            var index = items.Select((item, i) => (item, i)).First(entry => ReferenceEquals(entry.item, pressed)).i;
            showLink(links[index]);
        }
        chain.CardPressed += OnPressed;
        chainSection.Show();
        return new Disposable(() =>
        {
            chain.CardPressed -= OnPressed;
            chain.Clear();
            chainSection.Hide();
        });
    }
}
