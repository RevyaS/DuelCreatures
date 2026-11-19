using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Actions;
using ArC.CardGames.Components;
using ArC.CardGames.Rules;
using ArC.CardGames.Skills;
using ArC.CardGames.YuGiOh;

/// <summary>
/// YuGiOh's windows (chain responses, priority, a summon): drag the effect to activate out of the list, one card per
/// offered activation (pressed, it shows that effect); close the list to pass. While a chain is building, its links are
/// shown above, each card showing the effect it activated, to read without closing the responses. A spell played from
/// the hand that could go to several zones then asks for the zone (tap it).
/// </summary>
public sealed class YuGiOhResponsesPrompt(YuGiOhInput input)
{
    public static class Text
    {
        public static string Respond = "Respond to the chain";
        public static string Priority = "Activate an effect?";
        public static string Summon = "Respond to the summon";
        public static string Chain = "Chain";
        public static string Zone = "Tap the zone to activate it in";
    }

    public async Task<Request> Answer(Offered offered, OpenWindow open)
    {
        var title = ReferenceEquals(open.Window, YuGiOhWindows.Responses) ? Text.Respond
            : ReferenceEquals(open.Window, YuGiOhWindows.Summon) ? Text.Summon
            : Text.Priority;

        // One card per activation; a spell from the hand once, whichever zones it could go to
        var options = new List<(CardBase Card, Request Request, ISkill? Skill)>();
        foreach(var activation in offered.Of<ActivateSkillRequest>()) options.Add((activation.Card, activation, activation.Skill));
        foreach(var set in offered.Of<ActivateSetRequest>()) if(set.Zone.Card is { } card) options.Add((card, set, null));
        foreach(var playable in offered.Of<PlayAbilityRequest>())
        {
            if(!options.Any(option => ReferenceEquals(option.Card, playable.Card) && option.Request is PlayAbilityRequest)) options.Add((playable.Card, playable, null));
        }
        if(options.Count == 0) return offered.Pass;

        using var shownChain = input.Chain.Count == 0
            ? null
            : Prompts.ShowToInspect(input.ChainView, Text.Chain, input.Chain.Select(link => (link.Activation.Card, link)).ToList(),
                (card, link) => input.ShowDetails(new CardDetail(card) { Focus = link.Skill }));

        var (picked, chosen) = await Prompts.DragOutOfCardList(input.EffectPicker, title,
            options.Select(option => (option.Card, option)).ToList(), canDecline: true,
            (card, option) => input.ShowDetails(new CardDetail(card) { Focus = option.Skill }));
        if(!picked) return offered.Pass;
        if(chosen.Request is not PlayAbilityRequest play) return chosen.Request;

        // A spell from the hand: the zone, when it could go to several
        var plays = offered.Of<PlayAbilityRequest>().Where(candidate => ReferenceEquals(candidate.Card, play.Card)).ToList();
        if(plays.Count == 1) return plays[0];
        var zone = (await YuGiOhChoices.ChooseZones(input, plays.Select(candidate => (ICardSpace)candidate.To).ToList(), 1, 1, Text.Zone)).Single();
        return plays.First(candidate => ReferenceEquals(candidate.To, zone));
    }
}
