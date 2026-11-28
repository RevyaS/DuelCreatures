using System;
using System.Linq;
using ArC.CardGames.Components;
using Godot;

/// <summary>
/// The input screens every game's input provider has in its PromptLayer, by unique name: %FieldSelect (circles, zones,
/// units on the enlarged field), %CardSelect (cards from anywhere else) and %SwipeUp (one skill or option). A card tapped
/// on any of them shows its details (<see cref="Find"/>'s showDetails).
/// </summary>
public sealed record InputScreens(FieldSelectScreen FieldSelect, CardSelectScreen CardSelect, SwipeUpPicker SwipeUp)
{
    /// <summary>The screens under <paramref name="owner"/>, a tapped card's details going to <paramref name="showDetails"/>.</summary>
    public static InputScreens Find(Node owner, Action<CardDetail> showDetails)
    {
        var screens = new InputScreens(
            owner.GetNode<FieldSelectScreen>($"%{nameof(FieldSelect)}"),
            owner.GetNode<CardSelectScreen>($"%{nameof(CardSelect)}"),
            owner.GetNode<SwipeUpPicker>($"%{nameof(SwipeUp)}"));
        screens.FieldSelect.CardPressed += card => showDetails(new CardDetail(card));
        screens.CardSelect.CardPressed += card => showDetails(new CardDetail(card));
        return screens;
    }

    /// <summary>Gives the field selection screen the board it enlarges (once the game is shown).</summary>
    public void Attach(InputBoard input, ArcDriver driver, Control boardRoot) => FieldSelect.Attach(input, driver, boardRoot);

    /// <summary>Makes every ResolvingSkillDetails under <paramref name="owner"/> (the pinned one, the screens' banners) show <paramref name="skills"/>.</summary>
    public static void FollowSkills(Node owner, ResolvingSkills skills)
    {
        foreach(var resolving in owner.Descendants<ResolvingSkillDetails>()) resolving.Follow(skills);
    }
}
