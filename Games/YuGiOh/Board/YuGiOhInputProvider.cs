using ArC.CardGames;
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

    public YuGiOhPlayArea PlayArea { get; private set; } = null!;

    public YuGiOhPlayArea OpponentPlayArea { get; private set; } = null!;

    public YuGiOhSkillService SkillService { get; private set; } = null!;

    PlayAreaBase IPlayerInputProvider.PlayArea => PlayArea;

    IInputProviderStrategy strategy = null!;
    Stack<IInputProviderStrategy> strategyStack = new();

    public override void _Ready()
    {
        board = GetNode<YuGiOhBoard>($"%{nameof(YuGiOhBoard)}"); 
    }

    public void Activate(YuGiOhPlayArea playArea, YuGiOhPlayArea opponentPlayArea, YuGiOhSkillService skillService)
    {
        PlayArea = playArea;
        OpponentPlayArea = opponentPlayArea;
        SkillService = skillService;
    }

    public void SetEventBus(YuGiOhEventBus eventBus)
    {
        eventBus.PhaseChanged += OnPhaseChanged;
    }

    private Task OnPhaseChanged(IPhase phase)
    {
        if(phase is not IManualPhase) return Task.CompletedTask;

        switch(phase)
        {
            case MainPhase:
                SetProviderStrategy(new YuGiOhMainPhaseStrategy(board));
                return Task.CompletedTask;
        }
        throw new NotSupportedException($"{phase.GetType().Name} is not supported yet");
    }

    public void SetProviderStrategy(IInputProviderStrategy strategy)
    {
        strategyStack.Clear();
        strategyStack.Push(strategy);
        SetProviderStrategyCore();
    }

    public void PushProviderStrategy(IInputProviderStrategy strategy)
    {
        strategyStack.Push(strategy);
        SetProviderStrategyCore();
    }

    public void PopProviderStrategy()
    {
        strategyStack.Pop();
        SetProviderStrategyCore();
    }

    private void SetProviderStrategyCore()
    {
        strategy = strategyStack.Peek();
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

    public async Task<IMainPhaseAction> RequestMainPhaseAction(List<IMainPhaseAction> actions)
    {
        return await ((IRequestMainPhaseAction)strategy).RequestMainPhaseAction(actions);
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

    public async Task<YuGiOhChainResponse?> SelectChainResponse(YuGiOhChain chain)
    {
        return await ((ISelectChainResponse)strategy).SelectChainResponse(chain);
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

    public Task<string> DeclareCardName()
    {
        throw new NotImplementedException();
    }
}
