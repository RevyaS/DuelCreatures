using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Rules;
using ArC.CardGames.Setup;
using ArC.CardGames.YuGiOh;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class YuGiOhInputProvider : Control, IYuGiOhPlayerInputProvider
{
    YuGiOhBoard board = null!;
    public YuGiOhBoard Board => board;

    public YuGiOhPlayArea PlayArea => throw new NotImplementedException();

    public YuGiOhPlayArea OpponentPlayArea => throw new NotImplementedException();

    public YuGiOhSkillService SkillService => throw new NotImplementedException();

    PlayAreaBase IPlayerInputProvider.PlayArea => PlayArea;

    public override void _Ready()
    {
        board = GetNode<YuGiOhBoard>($"%{nameof(YuGiOhBoard)}"); 
    }

    public Task<bool> QueryActivateSkill(YuGiOhCard invoker, YuGiOhAutomaticSkill skill)
    {
        throw new NotImplementedException();
    }

    public Task<bool> QuerySetCard<TCard>(TCard card) where TCard : YuGiOhCard
    {
        throw new NotImplementedException();
    }

    public Task<IAttackPhaseAction> RequestAttackPhaseAction(List<IAttackPhaseAction> actions)
    {
        throw new NotImplementedException();
    }

    public Task<IMainPhaseAction> RequestMainPhaseAction(List<IMainPhaseAction> actions)
    {
        throw new NotImplementedException();
    }

    public Task<YuGiOhCard> SelectCardFromDeck()
    {
        throw new NotImplementedException();
    }

    public Task<YuGiOhCard> SelectCardFromExtraDeck()
    {
        throw new NotImplementedException();
    }

    public Task<YuGiOhCard> SelectCardFromGraveyard()
    {
        throw new NotImplementedException();
    }

    public Task<CardBase> SelectCardFromHand()
    {
        throw new NotImplementedException();
    }

    public Task<CardBase?> SelectCardFromHandOrNot()
    {
        throw new NotImplementedException();
    }

    public Task<YuGiOhCard> SelectCardFromOpponentGraveyard()
    {
        throw new NotImplementedException();
    }

    public Task<YuGiOhCard> SelectCardFromOpponentHand()
    {
        throw new NotImplementedException();
    }

    public Task<List<CardBase>> SelectCardsFromHandRange(int minimum, int maximum)
    {
        throw new NotImplementedException();
    }

    public Task<YuGiOhChainResponse?> SelectChainResponse(YuGiOhChain chain)
    {
        throw new NotImplementedException();
    }

    public Task<IYuGiOhZone> SelectOpponentZone(ZoneSelector selector)
    {
        throw new NotImplementedException();
    }

    public Task<IYuGiOhZone> SelectOwnZone(ZoneSelector selector)
    {
        throw new NotImplementedException();
    }

    public Task<List<IYuGiOhZone>> SelectOwnZonesRange(ZoneSelector selector, int minimum, int maximum)
    {
        throw new NotImplementedException();
    }

    public Task<SkillParts<YuGiOhSkillContextObject, YuGiOhCard, YuGiOhSequenceObject>.SkillAction> SelectSkillOption(IReadOnlyList<SkillParts<YuGiOhSkillContextObject, YuGiOhCard, YuGiOhSequenceObject>.SkillAction> options, YuGiOhSkillContextObject context)
    {
        throw new NotImplementedException();
    }

    public Task<YuGiOhNonPassiveSkill> SelectSkillToActivate(List<YuGiOhNonPassiveSkill> skills)
    {
        throw new NotImplementedException();
    }

    public Task<List<YuGiOhCard>> SelectXYZMaterials(MonsterZone zone, int amount)
    {
        throw new NotImplementedException();
    }
}
