# Adopting the rewritten ArC in DuelCreatures

A guide for whoever does the migration: you, or a new Claude session. It explains what DuelCreatures is made of, which
parts of it talk to ArC, what the rewrite changes for each of them, and how to keep the touch gestures exactly as they
are while ArC now asks its questions as menus.

What this guide does **not** explain: how ArC works inside (requests, rules, windows, the skill engine, stats). That
is ArC's own documentation, and you should read it there rather than here:

| You want to know | Read |
|---|---|
| What ArC is now, its vocabulary, where things live | `../ArC/CLAUDE.md` |
| Why it is shaped this way, what's built, what's still open | `../ArC/ArC.CardGames/Design - Authoring redesign.html` (Status, Left for you, Still open) |
| What happens inside when a game runs | `../ArC/ArC.CardGames/Guide - Engine internals.md` |
| DuelCreatures' own history and decisions | `docs/ArC-Workflow-Plan.md` |
| How a board scene is built and bound | `docs/New-Game-Board-Guide.md` |

The behaviour spec, the list of gestures that must keep working, is **"Input scenarios to keep"** in `CLAUDE.md`. This
guide refers to it rather than repeating it.

---

## 1. The change in one page

Before the rewrite, ArC asked each game's own questions, one method per question: `RequestMainPhaseAction`,
`SelectOwnZone`, `SelectCardFromHand`, `SelectXYZMaterials`, `SelectChainResponse`, and so on. An action like "Normal
Summon" was an *intent*: once chosen, ArC asked its follow-up questions (which card, which zone, which tributes).
DuelCreatures decided what was legal by calling `YuGiOhGameRules.Can…` / `VanguardGameRules.Validate…` itself, and a
gesture answered several questions at once through **pre-answers**.

After the rewrite, every game asks the same **four questions** (`IPlayerInput`):

| Question | Asks | Typical use |
|---|---|---|
| `ChooseAction(ActionChoice)` | "What do you do now?" | Phase menus, response windows, the guard step |
| `Choose<T>(Choosing<T>)` | "Pick from these" | Skill targets, costs, searches, trigger targets, materials, mulligan |
| `Confirm(Confirmation)` | "Yes or no?" | "Use this skill?", "you may" |
| `Order<T>(Ordering<T>)` | "In what order?" | Game procedures (none of the two games asks yet) |

And three things that matter most for the client:

1. **Menus offer complete requests.** `ChooseAction` lists every request the player may make now, already filled in:
   `NormalSummonRequest(monster, zone, tributes)`, `CallRequest(card, circle)`, `AttackRequest(attacker, target,
   boosted)`. Each one already passed ArC's rules. So **legality comes from what is offered**, not from the client
   calling rule queries.
2. **The answer must be one of the offered objects, by reference.** Every question has an `Expect` that throws if the
   answer wasn't offered. Never build a request yourself; find the one ArC offered.
3. **Every question says what it is for.** `ActionChoice.Where` is the phase (or the open window); `Choosing<T>.Purpose`,
   `Confirmation.Purpose` and `Ordering<T>.Purpose` are the skill step, the request, the resolution policy, or an enum
   such as `TriggerQuestion.Power` or `MulliganQuestion.RETURN`.

ArC's roadmap says "DuelCreatures: a generic action menu over the four questions". **We don't do that.** ArC thinks in
menus because menus are the most general answer for bots and tests. DuelCreatures is touch-first: the menu is never
shown. It is the *source of truth* the gestures are checked against. A list of request names on screen is not an
acceptable answer to any question; see section 4.

---

## 2. What DuelCreatures is made of

A tour for someone new. For each part: what it does, what it can do for a new game, and how it touches ArC.

```
ArC session ── CardsMoved / CardTurned / DeckShuffled ──▶ ArcDriver ──▶ zone views (IZoneView) ──▶ Presenter (animations, in order)
            ── typed game events (PhaseStarted, AttackDeclared, ...) ──▶ session components (labels, battle lines, pinned skill)
            ◀── answers ── I…PlayerInputProvider ◀── strategies (InputScope + gestures) ◀── InputBoard ◀── gestures on views
```

### 2.1 The board: showing what ArC did (`Games/Shared/Board/`)

- **Zone scenes** (`Zones/`): `CardSlot` (one card, never sideways), `CardBox` (one card, may turn sideways),
  `LineZone` (a row of slots), `HandZone` (a growing row), `StackZone` (a pile with a count, may list its cards). A
  zone scene draws cards and raises gestures. It knows nothing about any game.
- **`ArcDriver`** binds zone views to ArC zones **by node name**: a unique node `%Graveyard` shows the play area's
  `Graveyard` property. At startup it fails once with a `BindingException` listing every missing, extra or mismatched
  node. While the game runs, it turns ArC's `CardsMoved`, `CardTurned` and `DeckShuffled` into `ZoneChange`s and hands
  them to the `Presenter`, which plays them one after another. It picks animations, sounds and pauses by the move's
  reason (`Animate<TReason>`, `Sound<TReason>`, `Hold<TReason>`; the lookup walks the reason's **base classes**, not
  its interfaces). In debug builds it compares every view with its ArC zone at each settle and logs desyncs.
- **Extensions** are scripts under a zone node that implement `IBindable<TZone>` (Vanguard's `UnitStats`,
  `GuardianShields`). **Session components** implement `IBindable<TSession>` and are bound by
  `driver.BindAll<GameSession>` (`PhaseLabel`, `TurnLabel`, `LifePointsLabel`, the battle lines). They get context from
  their ancestors with `DependOn<T>()`.

ArC touchpoints: zone **property names** (`ZonesByName`), `CardsMoved`, `CardTurned`, `DeckShuffled`, the move
reasons, `EventBus.Subscribe<T>` and `EventBus.Settled`, the game events listed in 5.2.

### 2.2 Input: turning gestures into answers (`Games/Shared/Input/`)

- **Gestures** (`Gestures.cs`) are records in game terms, carrying ArC cards and zones, never nodes: `CardClicked`,
  `CardLongPressed`, `ZoneClicked`, `CardDropped` (with the `DropChoice` region it landed on), `ZoneSwiped`, `Aimed`
  (`ZoneTarget` / `AreaTarget`, optional `Via` for a boost), `ButtonPressed`, `SkillActivated`, `CommandPressed`.
- **`InputBoard`** holds the HUD's buttons, the hint label and the aim layer, and receives gestures from the driver.
- **`InputScope`** is what a strategy opens to say what may happen now: `Cards`, `Zones`, `Drag`, `Drop` (with
  labelled regions such as **Summon | Set**), `Swipe`, `Aim`, `LongPress`, `Button`, `Numbered` (pick badges 1, 2, …),
  `Activatable` (which skill buttons are live in the card details), `Commands` (other buttons in the details, like
  **Xyz Summon**), `Hint`. `await scope.Next()` returns only gestures the scope allowed. Disposing the scope undoes every
  highlight, flag and button.
- **`Prompts`** has the shared prompts: `PickCards` (pick several from a list, Confirm), `DragOutOfCardList` (press to
  read, drag out to pick, drop back to return), `ShowToInspect` (a read-only list, used for the chain), `Choose` (a
  dialog of options).

These are all ArC-independent except for the card and zone types they carry. **They survive the migration almost
untouched.** They are the reason the gestures can stay the same while the questions change.

### 2.3 Per game: strategies (`Games/<Game>/Input/`, `Games/<Game>/Board/<Game>InputProvider.cs`)

Today each game has:

- `<Game>Questions`: one typed `Question<T>` key per old provider method;
- `I<Game>Answers` and `<Game>Strategy`: one virtual method per old question, passing down to the one below;
- `Default<Game>Answers`: answers every question with a plain prompt;
- phase strategies (`YuGiOhMainPhaseStrategy`, `VanguardAttackPhaseStrategy`, …): open an `InputScope`, wait for a
  gesture, return the matching intent action and **pre-answer** its follow-up questions;
- `<Game>InputProvider`: the Godot node that is the human player. For each ArC question: pre-answer, else the phase
  strategy, else the defaults, after `Presenter.WhenIdle()`.

**This is the layer the rewrite replaces.** Its *questions* (`Question<T>`, `PreAnswers`, `Narrowing`,
`I<Game>Answers`) are built around the old one-method-per-question interface and go away. Its *gestures* (every
`scope.Drag/Drop/Aim/…` and every `case CardDropped …`) are the spec and are kept. Section 3 and 4 show how.

### 2.4 HUD (`Games/Shared/HUD/`)

- **Card details** are built from parts (`CardNamePart`, `CardValuePart`, `SkillTextPart`, `GrantedSkillsPart`,
  `ActivationListPart`, `CardCommandsPart`), one scene per game. Skill buttons ask an **`IActivationGate`**, which the
  current scope provides (`scope.Activatable(...)`); with no scope everything is denied (the deck builder).
- **`PromptLayer`** holds the prompt panels; swipe down collapses them to a chevron bar, swipe up or tap restores.
- **`ResolvingSkillDetails`** pins the player's own skill while it is activated or resolved.
- **`ChainView`** (YuGiOh) shows the chain so far beside the responses.

ArC touchpoints: skill text (the old `ISkillTextInterpreter`), `IManuallyActivatedSkill`, granted skills, the skill
start/end events, the chain.

### 2.5 Looks and setup

`CardGameStyle` resources, sleeves, card faces (`Games/CardGames.cs`, `Games/<Game>/<Game>Style.tres`) read card
*types* only. Sessions (`YuGiOhSession.cs`, `VanguardSession.cs`) build the game with the session builders and run it
through `SessionLoop.Run`. Bots come from ArC (`VanguardOpponents`, the Exosister bot).

### 2.6 What survives, at a glance

| Part | Survives? | What changes |
|---|---|---|
| Zone scenes, `IZoneView`, `Presenter`, flights, sleeves, card faces | As is | Nothing |
| `ArcDriver` binding by name, desync check | As is | Nothing, unless a zone property is renamed in ArC |
| `ArcDriver` move animations (`Animate<TReason>`) | Re-map | YuGiOh: some reasons are now requests. Vanguard: unchanged until ArC's slice G |
| Gestures, `InputBoard`, `InputScope`, `Prompts`, `DropChoicesOverlay`, aiming, badges | As is | Nothing |
| `Question<T>`, `PreAnswers`, `Narrowing`, `I<Game>Answers`, `<Game>Strategy`, `<Game>Questions` | **Replaced** | By per-question handlers over the four questions (section 3) |
| Phase strategies | **Rewritten inside, same gestures** | They match gestures against offered requests instead of rule queries (section 4) |
| `Default<Game>Answers` | **Replaced** | By a router that answers by *shape* of the question (section 4.4) |
| `<Game>InputProvider` | Rewritten | Implements the four questions; `SetEventBus`/`Activate` change |
| Card details, `IActivationGate` | Kept, gate rewritten | The gate checks the offered `ActivateSkill`s |
| `ISkillTextInterpreter`, `VanguardSkillTextInterpreter` | **Gone in ArC** | Skill text shows `Skill.Name` until ArC has text (section 7) |
| `ResolvingSkillDetails` | Kept | Fed by `SkillUseStarted`/`SkillUseEnded` |
| `ChainView` | Kept | Fed by `ChainLinkAdded`/`ChainResolved`, or the open window's `About` |
| Session builders, `SessionLoop` | Kept | The services record and provider interfaces changed shape |

---

## 3. The new input provider

### 3.1 The shape

Each game's provider is still the Godot node that is the human player, and still implements the game's provider
interface. That interface is now just the four questions plus the play areas:

```csharp
// ArC (for reference, not to change)
public interface IYuGiOhPlayerInputProvider : IPlayerInputProvider   // IPlayerInputProvider : IPlayerInput
{
    new YuGiOhPlayArea PlayArea { get; }
    YuGiOhPlayArea OpponentPlayArea { get; }
}
// IVanguardPlayerInputProvider is the same with VanguardPlayArea.
```

So the provider has four entry points instead of ~20. Each one **routes** the question to whoever answers it, waits for
the board to be idle first (`Presenter.WhenIdle()`, as today), and checks the answer before returning it:

```csharp
// Sketch: the shape of every game's provider after the migration
public async Task<Request> ChooseAction(ActionChoice choice)
{
    await Presenter.WhenIdle();
    var chosen = await actions.Answer(choice);          // routed by choice.Where (3.2)
    return choice.Expect(chosen);                       // fails loudly if a strategy built its own request
}

public async Task<IReadOnlyList<T>> Choose<T>(Choosing<T> question)
{
    await Presenter.WhenIdle();
    return question.Expect(await picks.Answer(question)); // routed by Purpose, T and where the items are (3.3)
}
// Confirm and Order the same way.
```

Calling `Expect` yourself as well as ArC is cheap and gives the error at the client's call site, with the strategy's
name in the stack.

### 3.2 Routing `ChooseAction`: by `Where`

`choice.Where` tells you who is asking. That replaces subscribing to `PhaseStarted` just to swap strategies (keep
`PhaseStarted` for labels):

| `Where` is | Game | Handler (keeps today's gestures) |
|---|---|---|
| `YuGiOhMainPhase` (`YuGiOhMainPhase1` / `2`) | YuGiOh | `YuGiOhMainPhaseStrategy` |
| `YuGiOhBattlePhase` | YuGiOh | `YuGiOhBattlePhaseStrategy` |
| `OpenWindow { Window: YuGiOhWindows.Responses }` | YuGiOh | chain responses (drag out of the list, close to pass, `ChainView` above) |
| `OpenWindow { Window: YuGiOhWindows.Priority }` | YuGiOh | same gesture as responses, no chain yet (between phases, after an attack declaration) |
| `RidePhase` | Vanguard | `VanguardRidePhaseStrategy` |
| `VanguardMainPhase` | Vanguard | `VanguardMainPhaseStrategy` |
| `VanguardAttackPhase` | Vanguard | `VanguardAttackPhaseStrategy` |
| `OpenWindow { Window: VanguardAttackPhase.GuardStep }` | Vanguard | the guard prompt (`VanguardGuardPrompt`), `About` is the `AttackRequest` |

An unknown `Where` must **throw**, naming it and listing the offered request types. That's how a new phase or window
in ArC shows up: as a loud error the first time it asks, not as a silent menu.

Leaving: `EndPhaseRequest` (phase menus) and `PassRequest` (windows) are in `Available` too. Next Phase returns the
offered `EndPhaseRequest`; closing the response list returns the offered `PassRequest`. YuGiOh's **End Phase** button
returns the offered `SkipToEndPhaseRequest`.

### 3.3 Routing `Choose<T>`, `Confirm`, `Order`: by purpose, type and place

These come from three sources, and the purpose tells you which:

| Purpose is | Comes from | Example |
|---|---|---|
| an enum value | a game procedure | `MulliganQuestion.RETURN`, `TriggerQuestion.Power/Critical/Stand/Heal` |
| a `Pick<T>` / `Ask` step | a skill (its cost or effect) | Counter Blast, a deck search, a target |
| an `ActivateSkill` | the skill engine: "Use X?" for an optional trigger | `Confirm` |
| an `IResolutionPolicy` | several triggers waiting, the player chooses the next | `Choose<ActivateSkill>` |
| a request or an event | a procedure about it | YuGiOh: where a monster whose control is returned comes back (`Choosing<MonsterZone>`, purpose the control-taken event) |

Skill steps don't say *which* cost they are: a Counter Blast and a Soul Blast are both a `Pick<CardBase>` with a
prompt. You tell them apart by **where the offered items are**: face-up cards in your damage zone, cards in your soul,
cards in your hand. That is also the right way to choose the gesture, because the gesture depends on where the cards
are, not on the skill. So the router looks at, in order:

1. a known enum purpose (the procedures' questions, each with its fixed gesture);
2. `T` (zones and circles, cards, skills, anything else);
3. for cards, the zone holding each offered card (find it with the driver's views or the play areas' `ZonesByName`, by
   reference).

`Choose<T>` is generic, so the router needs one type test per `T` it handles and a cast back:

```csharp
// Sketch: the one place a cast is unavoidable; keep it in this file only
public async Task<IReadOnlyList<T>> Answer<T>(Choosing<T> question) => question switch
{
    Choosing<UnitCircle> circles  => (IReadOnlyList<T>)await PickZones(circles),
    Choosing<RearGuard> circles   => (IReadOnlyList<T>)await PickZones(circles),
    Choosing<MonsterZone> zones   => (IReadOnlyList<T>)await PickZones(zones),
    Choosing<ActivateSkill> next  => (IReadOnlyList<T>)await PickSkill(next),
    _ when IsCards(question)      => (IReadOnlyList<T>)await PickCards(question), // CardBase, VanguardCard, YuGiOhCard, ...
    _ => throw new UnansweredQuestion(question.Purpose, question.Prompt, typeof(T)),
};
```

Questions about the *opponent's* choice ("your opponent chooses", `Asker.Opponent`) arrive at the opponent's provider:
the bot's, or yours when the bot's skill asks you. Your provider must answer them like any other.

### 3.4 What happens to pre-answers

They aren't needed for menus any more: a menu's request already carries the card, the zone and the tributes. The
gesture picks the request, and that's the whole answer.

What's left are **gestures that take several steps before one answer**: a tribute summon (drop, then tap tributes,
Confirm), an Xyz Summon (press the command, tap materials, Confirm), a boosted attack (drag through the booster), the
guard (stage several guardians, Confirm). These stay inside one strategy: it narrows the offered requests with each
gesture until exactly one is left, then returns it (section 4.2).

So `Question<T>`, `PreAnswers`, `Narrowing`, `I<Game>Answers`, `<Game>Strategy` and `<Game>Questions` can be
deleted once nothing uses them. The safety they gave ("the action asked different questions than the strategy
expected") is now given by `Expect` (a request that wasn't offered) and by the router's throw (a question nobody
handles).

---

## 4. Keeping the gestures: matching gestures to offered requests

### 4.1 The pattern

(Section 12 walks through writing one from scratch, with the scene side and the selection panels.)

Every phase strategy becomes the same three moves:

1. **Allow** only what an offered request could use: a card can be dragged if some offered request uses it; a zone
   accepts a drop if some offered request puts that card there; the drop region (**Summon**, **Set**, **Activate**,
   **Call**, **Ride**) is shown if a request of that kind is offered for that card and zone.
2. **Wait** for one gesture (`scope.Next()`).
3. **Find** the offered request that the gesture describes, and return it.

A small shared helper over `choice.Available` keeps this readable and typed (a sketch; name it as you like):

```csharp
// Sketch: Games/Shared/Input/Offered.cs
public sealed class Offered(ActionChoice choice)
{
    public IEnumerable<TRequest> Of<TRequest>() where TRequest : Request => choice.Available.OfType<TRequest>();
    public bool Any<TRequest>(Func<TRequest, bool> match) where TRequest : Request => Of<TRequest>().Any(match);

    /// <summary>The one offered request matching; throws naming the gesture when there are none or several.</summary>
    public TRequest One<TRequest>(Func<TRequest, bool> match, string gesture) where TRequest : Request { ... }

    public bool Uses(Request request, CardBase card) => request.Cards.Any(one => ReferenceEquals(one, card));
    public EndPhaseRequest EndPhase => Of<EndPhaseRequest>().Single();
    public PassRequest Pass => Of<PassRequest>().Single();
}
```

Compare cards and zones by reference everywhere (`ReferenceEquals`), as the conventions already say: two copies of a
filler card are equal records.

### 4.2 YuGiOh, request by request

The menus (`YuGiOhMainPhase.Menu`, `YuGiOhBattlePhase.Menu`) allow these requests. Each row keeps the gesture in
`CLAUDE.md` and says how to find its request.

| Gesture (unchanged) | Offered request | How the strategy uses it |
|---|---|---|
| Drag a monster from the hand onto a monster zone, **Summon \| Set** | `NormalSummonRequest` / `SetMonsterRequest` (`Monster`, `Zone`, `Tributes`) | Draggable: some request has that `Monster`. Regions on a zone: **Summon** if a `NormalSummonRequest` has that monster and zone, **Set** if a `SetMonsterRequest` does. On drop: keep the requests of the chosen kind with that monster and zone. One left: return it. Several (they differ by `Tributes`): tap the tributes (numbered, Confirm), return the one whose `Tributes` are the tapped zones |
| Drag a spell onto a spell & trap zone, **Activate \| Set**; a trap, **Set** | `PlayAbilityRequest(Card, To)` / `SetCardRequest(Card, To)` | **Activate** if a `PlayAbilityRequest` has that card and zone; **Set** if a `SetCardRequest` does. A trap simply has no `PlayAbilityRequest` offered: ArC's rules (`TrapsAreSetFirst`) decide that, not the strategy |
| Swipe left (Defense) / right (Attack) | `ChangePositionRequest(Zone)` | Allowed if one is offered for the zone **and** the direction fits the card's current orientation (reading the board for direction is the client's job; legality isn't) |
| Swipe right on a face-down monster | `FlipSummonRequest(Zone)` | Allowed if one is offered for the zone |
| Tap a card, press an effect in its details | `ActivateSkill(Card, From, Skill)` | The activation gate allows a button when an offered `ActivateSkill` has that card, place and skill. Pressing it returns that request |
| Press a Set card's effect | `ActivateSetRequest(Zone)` | The gate allows the Set card's `Effect` when an `ActivateSetRequest` for its zone is offered; pressing returns it |
| Extra Deck list → monster → **Xyz Summon** → materials (numbered, Confirm) | `XyzSummonRequest(Monster, Materials, Zone)` | The command shows when some request has that monster. Material taps narrow the requests (only zones that appear in a remaining request can be tapped). On Confirm, keep those whose `Materials` are exactly the tapped zones; if they still differ by `Zone`, tap the zone; return the one left |
| **Next Phase** / **End Phase** | `EndPhaseRequest` / `SkipToEndPhaseRequest` | Buttons shown only when offered |
| Drag from a monster onto an opponent's monster | `DeclareAttackRequest(Attacker, Target)` | Aim from zones that are some request's `Attacker`; a target is valid when a request has that attacker and target |
| …onto the opponent's **hand** (direct attack) | `DeclareAttackRequest(Attacker, Target: null)` | The hand is a valid target when a request with that attacker and `Target == null` is offered |
| Chain responses: drag an effect out of the list, close it to pass | `ActivateSkill`, `ActivateSetRequest`, `PlayAbilityRequest` in the `Responses` / `Priority` windows, plus `PassRequest` | One card in the list per offered request (`Request.Cards`), its details focused on that request's skill. Dragged out: return that request. Closed: return `PassRequest` |

What is gone from the strategies: every `YuGiOhGameRules.CanNormalSummonNow`, `CanSetFromHand`,
`CanActivateSpellFromHand`, `Usable…Skills`, `UsableSetCardActivations`, `XYZMaterialOptions`, `CanAttack`,
`CanChangeBattlePosition`, `CanFlipSummon` call. Most of those methods no longer exist in ArC; the offered requests
answer the same questions, and they can't disagree with what ArC will accept.

### 4.3 Vanguard, request by request

Menus: `RidePhase.Menu` (`RideRequest`), `VanguardMainPhase.Menu` (`CallRequest`, `SwapRequest`, `ActivateSkill`),
`VanguardAttackPhase.Menu` (`AttackRequest`), and the window `VanguardAttackPhase.GuardStep` (`CallGuardianRequest`,
`InterceptRequest`).

| Gesture (unchanged) | Offered request | How the strategy uses it |
|---|---|---|
| Drag a card onto the vanguard (**Ride**); Next Phase not to ride | `RideRequest(Card)`, `EndPhaseRequest` | Draggable: some `RideRequest` has the card |
| Drag from the hand onto a rear-guard circle (**Call**, overcall too) | `CallRequest(Card, Circle)` | **Call** on a circle when a request has that card and circle. Overcall is just a `CallRequest` onto an occupied circle |
| Drag a rear-guard onto the other circle of its column (**Move**) | `SwapRequest(From)` | The request names only the circle moved from; the drop target is its column's other circle. Allowed when a request has that `From` |
| [ACT] skills from the details | `ActivateSkill` | Same gate as YuGiOh |
| Drag from a front-row unit to a target | `AttackRequest(Attacker, Target, Boosted: false)` | Valid target: a request with that attacker and target |
| Boost: from the back-row unit over the unit in front, then to the target | `AttackRequest(…, Boosted: true)` | `Aim(attacker, via: booster, …)`; the boost line shows only when some request with that attacker has `Boosted` true |
| Guard (see 4.5) | `CallGuardianRequest(Guardian)`, `InterceptRequest(RearGuard)`, `PassRequest` | Staged on the board, sent on Confirm |
| Mulligan: drag hand cards into the selection panel, Confirm there | `Choose<CardBase>`, purpose `MulliganQuestion.RETURN` | Answer: the cards in the panel |
| Trigger: tap highlighted circles | `Choose<UnitCircle>` (`TriggerQuestion.Power` / `Critical`), `Choose<RearGuard>` (`Stand`), `Choose<VanguardCard>` (`Heal`, from the damage zone) | Highlight exactly the offered items |
| Counter Blast: long press damage cards to turn them, Confirm when exactly enough | `Choose<CardBase>` from a skill's cost; offered items are face-up cards in your damage zone | `LongPress` on the damage zone for the offered cards; Confirm when the count is within `Minimum`..`Maximum` |
| Counter Charge | `Choose<CardBase>`; offered are face-down damage cards | Same gesture |
| Soul Blast, discard: pick then Confirm | `Choose<CardBase>`; offered are soul / hand cards | `PickCards` for the soul (it's hidden); hand cards picked on the hand |
| Skill targets, interceptors: tap highlighted circles; several: numbered, Confirm | `Choose<UnitCircle>` / `Choose<RearGuard>` | `Zones(offered)` + `Numbered` |
| Deck searches: drag out of the card list | `Choose<VanguardCard>` / `Choose<CardBase>`; offered are deck cards | `DragOutOfCardList` |
| Skill order: drag out of the list | `Choose<ActivateSkill>`, purpose the resolution policy | One card per waiting skill, focused on that skill |

Vanguard's `VanguardGameRules` still has the old `Validate…`/`Can…` methods (the bots plan with them). **Don't use them
in strategies any more**: they can disagree with the menu, and ArC plans to let the bots plan from the offered
requests too.

### 4.4 Questions with no special gesture: answer by shape, never by a menu

`Default<Game>Answers` answered every old question with a plain prompt. Its replacement is the `Choose<T>` router of
3.3, and it picks a gesture from the **shape** of the question, all from the existing input pieces:

| The question offers | Gesture |
|---|---|
| zones or circles (`ICardSpace`) | highlight them, tap to pick; several: badges + Confirm (`Zones`, `Numbered`) |
| cards that are visible on the board (hand, field, face-up damage) | highlight them, tap to pick; several: badges + Confirm |
| cards in a hidden or piled zone (deck, soul, graveyard, banished, Extra Deck) | one: `DragOutOfCardList`; several: `PickCards` |
| skills (`ActivateSkill`) | `DragOutOfCardList`, one card per skill, details focused on it |
| yes / no (`Confirm`) | `ChoiceDialog` with the prompt, as today for optional triggers |
| an order (`Order<T>`) | (none asked yet) cards in a list, dragged into place |
| anything else (an enum, a number, a name) | **throw** `UnansweredQuestion`, naming purpose, prompt and type, until a gesture is designed for it |

A card list is fine: it shows **cards**, and the player picks by touching a card. What's not fine is a list of
*actions* as text ("Normal Summon", "Call", "Activate effect 2"). If a question can only be answered that way, the
question needs a gesture designed for it first; don't add a text menu as a fallback.

`Choosing<T>.CanCancel` adds "or nothing": closing the list (or Confirm with nothing picked) answers an empty list. When
`CanCancel` is false, closing must not be possible.

### 4.5 The guard step: previews, then requests

The spec says "nothing moves in ArC until Confirm (the board previews, then `Resync()`s)". The new guard step is a
**one-side window**: ArC asks `ChooseAction` again after every guardian or interceptor until the defender passes, and
each request it receives is performed at once. There's no request to take a guardian back. So the prompt has to keep
staging on the client:

1. First `ChooseAction` of the window: open the guard prompt as today. Dragging a hand card onto the guardian circle,
   back to the hand, tapping a front rear-guard to intercept, tapping it on the guardian circle to call it back, all
   change only a **staged list** and the board preview. Allowed moves come from the offered `CallGuardianRequest`s
   and `InterceptRequest`s. The would-be power under the guarded unit is the client's sum of the staged shields.
2. On Confirm: `Resync()` the previewed views, then answer this question with the **first** staged item's offered
   request (matched by `Guardian` / `RearGuard`, by reference).
3. Every following `ChooseAction` from the same window: answer the next staged item, matched against the **new**
   `Available` (the request objects are new each time, so match by card and circle, never keep the old instance).
4. Nothing left: answer `PassRequest`.

If a staged item is no longer offered when its turn comes (a skill triggered by an earlier guardian changed
something), fail loudly with the item and the offered list rather than skipping it. Questions asked between two guards
(a guardian's own skill) go through the router as usual.

### 4.6 The activation gate and commands

`IActivationGate.CanActivate(card, from, skill)` becomes: *an offered `ActivateSkill` has this card, this place and
this skill* (plus `ActivateSetRequest` for a Set card's effect). `Activate` returns that offered request as the
strategy's answer. The gate's `Changed` fires when a new `ChooseAction` opens a scope and when it closes, exactly as
now. `CardCommandsPart` (Xyz Summon) works the same way over `XyzSummonRequest`.

`IManuallyActivatedSkill` still exists (`Activated` implements it), so the details can still decide which skills get
a button. The game's `ISkillButtons` stays the place that says "a card activation only while Set".

---

## 5. Board and events

### 5.1 Moves and move reasons

`CardsMoved`, `CardTurned`, `DeckShuffled` keep their shape, and `ArcDriver` keeps consuming them. The reason is the
part that changes: **every `Request` is now an `IMoveReason`**, and a move made by a request carries the request as its
reason.

- **YuGiOh** maps no reasons today, so nothing breaks. When you add animations, map request types:
  `Animate<NormalSummonOrSetRequest>(…)`, `Animate<XyzSummonRequest>(…)`, `Animate<SetCardRequest>(…)`. The remaining
  YuGiOh reason records (`AttackHits`, `AttackLost`, `TributeCost`, `UsedAsXYZMaterial`, `CardActivated`, …) still
  exist for the moves no request makes directly.
- **Vanguard** still uses its own reasons (`Rode`, `Called`, `Overcalled`, `Swapped`, `DriveChecked`, `TookDamage`,
  `Healed`, `DefeatedInBattle`, `Guarded`, `Intercepted`, `GuardFinished`, `DeckedOut`, …). ArC's **slice G** will
  replace them with requests (`RideRequest`, `CallRequest`, `TriggerCheckRequest`, …). Until then
  `VanguardPresentationConfig.ShowVanguardMoves` keeps working; when G lands, re-map each line to the request type
  that now makes the move. `TriggerCheckRequest.Type` (drive or damage) is how a drive check and a damage check will be
  told apart.
- Moves made by skills carry the skill's request (`MoveRequest`, `MoveCardsRequest`, `FlipRequest`, …). Map
  `Animate<MoveRequest>` etc. once for both games if you want skill moves to look different from the default.
- A useful catch-all: `Animate<Request>` (the lookup walks base classes), for "any move a request made".

The view-completeness tests are still in ArC (`YuGiOhViewEventCompletenessTests`, `VanguardViewEventCompletenessTests`,
`EventOnlyBoard`). **Run them first**: they tell you whether the board can drift before you change anything here.

### 5.2 Game events the GUI listens to

| Today (DuelCreatures uses) | After the rewrite | Who listens |
|---|---|---|
| `PhaseStarted(Phase)` | `PhaseStarted(Phase, TurnPlayer)`; phase types are now `YuGiOhMainPhase1/2`, `YuGiOhBattlePhase`, `RidePhase`, `VanguardMainPhase`, `VanguardAttackPhase`, `MulliganPhase` | `PhaseLabel` (names), providers (only if still used to switch) |
| YuGiOh `AttackDeclared`, `BattleEnded` | Same | `YuGiOhBattleLines` |
| Vanguard `AttackDeclared`, `BoostDeclared`, `BattleEnded` | Same records (`AttackDeclared` still carries `AttackState`, `BoostDeclared` the `AttackRequest`) | `VanguardBattleLines` |
| `SkillStarting` / `SkillFinished` (`Owner`, `Source`, `Skill`, `Step`) | **`SkillUseStarted` / `SkillUseEnded`** (`Run`: `Player`, `Self`, `From`, `Skill`, `Activation`) | `ResolvingSkillDetails` |
| YuGiOh chain object in `SelectChainResponse` | `ChainLinkAdded(Link)`, `ChainLinkResolved(Link)`, `ChainResolved(Links)`; the `Responses` window's `About` | `ChainView` |
| Vanguard view events on `VanguardEventBus` | Don't use. ArC keeps them only until DuelCreatures stops | nobody |
| (stats read on settle) | `StatChanged<T>` also exists; reading on `Settled` still works | `UnitStats`, `GuardianShields`, `LifePointsLabel` |
| (none) | `WindowOpened` / `WindowClosed`, `HandMulliganed` | optional: e.g. a "Guard!" banner |

**Pinned skill with the chain.** With YuGiOh's chain, `SkillUseStarted` comes when the skill is activated (costs paid)
and `SkillUseEnded` when its link resolves, so a pinned skill would stay pinned across other players' responses. Use
`ChainLinkAdded` / `ChainLinkResolved` to tell "activating" from "resolving" if the pin should drop in between (the old
`Step`). In Vanguard (no chain) start and end wrap the whole use.

**Whose skill.** Compare `run.Player` with the provider's player profile (by reference), not the play area.

### 5.3 Stats and life points

Power, critical, shield (Vanguard) and ATK, DEF, Level, LP (YuGiOh) are stats now: on the card in play, or on the play
area for LP. The view extensions read them on `Settled` as today; the methods they call may have moved
(`UnitCircle.GetOverallPower` still reads the unit's stats, `GuardianCircleShield` sums guardians,
`YuGiOhGameRules.AttackOf/DefenseOf` read ATK/DEF). Prefer `StatService.ValueOf(target, stat)` with the game's stat
keys (`VanguardStats.Power`, `YuGiOhStats…`) when an old helper is gone.

---

## 6. Session setup

- **Builders.** `YuGiOhSessionBuilder` and `VanguardSessionBuilder` still take `Player1(profile, services => input)`
  and `Player2(...)`. The `…Services` records changed: `VanguardServices` is `Game`, `EventBus`, `DataQuery`, `Logger`
  (no effect service); `YuGiOhServices` is `Game`, `EventBus`, `EffectService`, `DataQuery`, `Logger` (no skill
  service). The Godot provider is still created lazily by the lambda.
- **Providers.** The YuGiOh provider's `Activate(playArea, opponentPlayArea, skillService)` and `SetEventBus` were the
  old wiring. Give the provider its play areas and the session's event bus however the builder now makes them
  available; it needs the play areas (interface properties), the event bus (for `SkillUseStarted` and the chain
  events), and the player profile (to compare with `Request.Player` / `run.Player`).
- **Bots** answer the four questions themselves; `VanguardOpponents` and the Exosister bot are used as before. The
  YuGiOh bot passes in response windows until its cards are on the engine (ArC's "Left for you").
- **`SessionLoop.Run`** reports game exceptions as before; a `RuleViolation` now names the broken rule
  (`violation.Rule.Description`), which is the first thing to print.

---

## 7. Card details and skill text

ArC removed the skill text interpreters (`ISkillTextInterpreter`, `VanguardSkillToStringInterpreter`,
`YuGiOhSkillTextInterpreter`). A skill is now data with a `Name`, costs and steps. Until ArC gives skills readable
text:

- move `ISkillTextInterpreter` into DuelCreatures (it's only used by `SkillTextPart`, `GrantedSkillsPart`,
  `ActivationListPart` and `CardDetailsView`) and make the default show `Skill.Name`;
- `CardDetail.FocusText` (one skill's text alone, used for picks and the pinned skill) shows the name too;
- ask ArC for skill text (see section 9).

"Choose one of" options were legacy `SkillAction` lists (`SelectSkillOption`). They don't exist in the new skill
engine; a skill that offers options will ask a `Choose<T>` of something. When such a skill is written, give its
purpose a gesture (most likely: one card per option dragged out of the list, as now), or it will hit the router's
throw.

---

## 8. Migration order

Each step ends with the Godot build passing (`dotnet build "Duel Creatures.csproj" -nologo -v q`) and, where it says so,
a scene for the user to run. The ArC references stay `<ProjectReference>` during all of this (D16), so the build shows
every place that broke.

| # | Step | Done when |
|---|---|---|
| 0 | Run ArC's tests, note what fails (`dotnet test ../ArC/ArC.CardGames.Tests -nologo`). Build DuelCreatures and save the error list: it's the work list | You have the list, grouped by file |
| 1 | **Board only.** Fix `ArcDriver`, zone binding, extensions and session components against the new events (5.1–5.3), with both input providers temporarily throwing `NotImplementedException` from the four questions | `VanguardBotSession.tscn` and `YuGiOhBotSession.tscn` (bot vs bot) render whole games with no desync errors |
| 2 | **Shared input.** Add `Offered`, the `Choose<T>` router with the shape gestures of 4.4, `UnansweredQuestion`, the activation gate over `ActivateSkill`. Remove `Question<T>`, `PreAnswers`, `Narrowing` once nothing uses them | Builds; nothing game-specific yet |
| 3 | **YuGiOh provider.** The four questions, `ChooseAction` routing by `Where`, main and battle strategies matched to requests (4.2), responses and priority windows, `ChainView` and the pinned skill on the new events. Delete `IYuGiOhAnswers`, `YuGiOhStrategy`, `YuGiOhQuestions`, `DefaultYuGiOhAnswers` | A game against the Exosister bot plays every YuGiOh gesture in `CLAUDE.md` (with the four sample cards on the engine; the rest have no skills yet) |
| 4 | **Vanguard provider.** Same for ride, main, attack (boost through `Aim … via`), the guard window with staging (4.5), triggers by `TriggerQuestion`, costs by where the offered cards are | A game against a trial deck bot plays every Vanguard gesture in `CLAUDE.md` |
| 5 | **Details and text.** Skill text from names (section 7); check the deck builder still shows details read-only | Inspecting works in game and in the deck builder |
| 6 | **Vanguard move reasons (when ArC's slice G lands).** Re-map `ShowVanguardMoves` to request types | The bot game's animations, sounds and pauses are as before |
| 7 | **Docs.** Update `CLAUDE.md` (the "What the client needs from ArC" list and the class names in "Input scenarios"), `docs/ArC-Workflow-Plan.md` (a decisions row: D1 and D2 are reversed by ArC offering complete requests) and `docs/New-Game-Board-Guide.md` (a new game's strategies match offered requests) | Docs describe the code |
| 8 | Step 9 of the workflow plan: back to DLL references | As written there |

Work a game at a time: YuGiOh first (its menus are the most complete on the new system), then Vanguard.

---

## 9. What to ask ArC for

Gaps found while writing this guide. None blocks the migration; each makes the client simpler or more certain.

| Need | Why | Workaround until then |
|---|---|---|
| Readable skill text | The details and the pinned skill can only show `Skill.Name` | Show the name |
| Telling cost picks apart without guessing (e.g. the `Cost` on the `Pick`'s purpose, or a named purpose per cost) | Counter Blast vs Counter Charge vs Soul Blast are all `Pick<CardBase>` | Decide by where the offered cards are (which is what the gesture needs anyway) |
| Vanguard slice G (requests as move reasons) | One way to map animations in both games | Keep mapping Vanguard's reason records |
| The skill being run on a `Pick`'s question | To pin the skill that asks without tracking `SkillUseStarted` / `Ended` | Track the current runs from the events |
| A guard step that can take a guardian back, or a request carrying every guardian at once | So the client needn't stage and replay | Stage and replay (4.5) |
| Window rules (typed `About`, planned in ArC) | `OpenWindow.About` is `object` today; the guard prompt casts it to `AttackRequest` | One cast, in the guard prompt |

---

## 10. Pitfalls

- **Returning a request you built.** `Expect` throws. Always return an object from `choice.Available` (or
  `question.Offered`).
- **Keeping a request across questions.** Each `ChooseAction` offers new objects. Re-match by card and zone.
- **`==` or `Contains` on cards, zones or requests.** Records compare by value; use `ReferenceEquals` and
  `ReferenceEqualityComparer`.
- **Deciding legality in a strategy.** If a gesture needs a rule query to decide whether it's allowed, the answer is
  usually already in what's offered. Strategies may read the board for *direction and placement* (which way a swipe
  goes, which circle is behind which), never for *permission*.
- **Swapping strategies on `PhaseStarted`.** Route by `choice.Where` instead: the question itself says who asks, and a
  window opened in the middle of a phase (the guard step, a priority check) is not a phase.
- **Silent fallbacks.** An unknown `Where`, purpose or `T` throws, naming it. A text menu is never the fallback.
- **Answering before the board caught up.** Keep `await Presenter.WhenIdle()` before every question.
- **Shadowed names.** In ArC's Vanguard namespace, `Skill` resolves to an old type; use `ArC.CardGames.Skills.Skill`. Watch
  for the same in `using` lists here.

---

## 11. Running the migration with Claude sessions

One session per step of section 8. A fresh session for each keeps its context small: it reads this guide and the
files of that step, not the whole history.

### Who does what

| You | Claude |
|---|---|
| Build, and paste **only the errors** (see the command below), grouped by file | Reads only the files the errors and the step name |
| Run scenes in Godot and say what you saw (which gesture, what happened, the error text) | Says which scene to run and what to look at |
| Run ArC's tests when a step needs them, paste the failures | Doesn't open ArC's design page; reads ArC's code only for the types it uses |
| Delete the files a step retires, edit scenes | Says which files and nodes, writes the code |
| Review and commit | Doesn't commit |

The error list, without the noise (paste its output):

```bash
dotnet build "Duel Creatures.csproj" -nologo -v q 2>&1 | grep -E "error" | sed -E 's/.*Games/Games/; s/ \[.*//' | sort -u
```

### The opening prompt

Copy it, set the step number, and paste the error list under it:

```text
We're adopting the rewritten ArC in DuelCreatures. Read docs/ArC-Rewrite-Adoption-Guide.md
(sections 1, 3, 4, 12 and the row for this step in section 8; the rest only if you need it).
Do step <N> only. Don't read ArC's design page or engine guide; read ArC code only for the
types you use. I build and run Godot myself: don't build, I'll paste errors.
Build errors for the files of this step:
<paste>
```

### Habits that save tokens

- **One step, then a new session.** When a step is done, ask the session to update the step's row in section 8 (and
  anything it found that the guide got wrong), then start the next one fresh.
- **Name the files.** "The YuGiOh main phase strategy: `Games/YuGiOh/Input/YuGiOhMainPhaseStrategy.cs`" is cheaper
  than "look at how YuGiOh input works".
- **Paste the error, not the log.** For Godot, the exception message and the top few frames of the stack.
- **Let it write; you check.** Ask for the change and a list of what to verify in Godot, not for explanations first.
- **Corrections stay in the session.** A wrong gesture is a small correction; a new topic is a new session.

---

## 12. Writing a gesture for a request

How to make one offered request playable by touch: what you need, how the pieces connect, what keeps everything else
switched off, and how to show a selection panel. The worked example is the Ride Phase.

### 12.1 What you need

| Piece | Where | For the Ride Phase |
|---|---|---|
| The request ArC offers, and who asks | ArC: the phase's or window's menu | `RidePhase.Menu` offers `RideRequest(Player, Card)`, plus `EndPhaseRequest`; `choice.Where` is the `RidePhase` |
| A view bound to every zone the gesture touches | The board scene: a unique node named like the zone property | `%Hand` (bound by the board script) and `%Vanguard` in the play area scene, already there |
| Any HUD button the gesture uses | A `BoardButtonView` with `Kind` set, anywhere under the provider's scene | `Kind = NextPhase`, already there |
| Where the hint shows | An `InputHintLabel` under the provider's scene | Each play area's `InputHint`, already there |
| The strategy | `Games/<Game>/Input/<Game><Phase>Strategy.cs` | `VanguardRidePhaseStrategy` |
| A route to it | The provider's `ChooseAction` router (3.2) | `RidePhase => ride.Answer(choice)` |

Nothing else: no binding code, no rule query, no extra scene node for the drop labels (a zone creates its
`DropChoicesOverlay` when a scope first offers a drop on it; place one in the zone's scene only to style it).

### 12.2 The strategy

```csharp
// Sketch: Games/Vanguard/Input/VanguardRidePhaseStrategy.cs on the new ArC
/// <summary>
/// The Ride Phase: drag a card from the hand onto the vanguard to ride it (only cards ArC offers a ride for can be
/// dragged), or Next Phase not to ride.
/// </summary>
public sealed class VanguardRidePhaseStrategy(VanguardInput input)
{
    public static class Text
    {
        public static string Ride = "Ride Phase: drag a card onto your vanguard to ride, or Next Phase";
    }

    public static class Choices
    {
        public static readonly DropChoice Ride = new("Ride");
    }

    public async Task<Request> Answer(ActionChoice choice)
    {
        var offered = new Offered(choice);
        var area = input.PlayArea;
        // The offered ride for this card, if there is one: the only legality check the strategy makes
        RideRequest? RideOf(CardBase card) => offered.Of<RideRequest>().FirstOrDefault(ride => ReferenceEquals(ride.Card, card));

        using var scope = input.Board.Scope()
            .Drag(area.Hand, card => RideOf(card) is not null)                              // 1. what can be picked up
            .Drop(area.Vanguard, card => RideOf(card) is not null ? [ Choices.Ride ] : [])  // 2. where it can go, labelled
            .Button(BoardButton.NextPhase)                                                  // 3. the way out
            .Hint(Text.Ride);

        switch(await scope.Next())                                                          // 4. one allowed gesture
        {
            case CardDropped drop:
                return RideOf(drop.Card)!;                                                  // 5. the offered request
            case ButtonPressed { Button: BoardButton.NextPhase }:
                input.Presentation.PlaySfx(VanguardSfx.PhaseChange);
                return offered.EndPhase;
            case var other:
                throw new InvalidOperationException($"{nameof(VanguardRidePhaseStrategy)} allowed {other} but doesn't handle it");
        }
    }
}
```

What each line does:

1. **`Drag(zone, allowed)`** makes the cards of the hand that `allowed` accepts draggable, and only those. A card ArC
   offers no ride for can't be lifted.
2. **`Drop(zone, choicesFor)`** makes the vanguard circle take a dragged card. While a card hovers, `choicesFor(card)`
   gives the regions to show (here one, **Ride**); an empty list means "doesn't take this card", so nothing lights up
   and letting go does nothing. Because the check is by the card's reference against offered requests, a card dragged
   from anywhere else simply doesn't match.
3. **`Button(kind)`** shows the HUD button for as long as the scope lasts.
4. **`Next()`** waits for a gesture **this scope allowed**. Everything else the player does meanwhile is ignored.
5. Return the request ArC offered, never a new one (`Expect` would throw).

**Another target zone is the same lines.** If ArC offered, say, a request to put a card from the hand into the damage
zone (a hypothetical `PutIntoDamageRequest(Player, Card)`), the strategy would be:

```csharp
PutIntoDamageRequest? PutOf(CardBase card) => offered.Of<PutIntoDamageRequest>().FirstOrDefault(put => ReferenceEquals(put.Card, card));
scope.Drag(area.Hand, card => PutOf(card) is not null)
     .Drop(area.DamageZone, card => PutOf(card) is not null ? [ Choices.PutIntoDamage ] : []);
```

The damage zone's view is a `LineZone`; every zone scene (`CardSlot`, `CardBox`, `LineZone`, `HandZone`, `StackZone`)
can take drops. If the zone has no view (`ViewOf` throws "No view shows this …"), it isn't bound: add the scene node
named like the zone property.

Several regions on one zone (YuGiOh's **Summon | Set**) are several `DropChoice`s in the list; `drop.Choice` says which
one the card landed on (compare with `ReferenceEquals`).

### 12.3 What keeps every other input off

You don't switch anything off by hand. The pieces are built so that **only what an open scope allowed is live**:

| Input | Off unless | Undone by |
|---|---|---|
| Dragging cards | `Drag` names the zone and the card passes `allowed` | disposing the scope (`SetDraggable(null)`) |
| Dropping on a zone | `Drop` names the zone and `choicesFor(card)` isn't empty | disposing (`OfferDrop(null)`) |
| Tapping to pick, highlights | `Cards` / `Zones` | disposing (`SetHighlight(false)`) |
| Swipes, aims | `Swipe` / `Aim` (the innermost open scope decides an aim in progress) | disposing (`SetGestures(false, false)`) |
| HUD buttons | `Button`; every `BoardButtonView` starts unavailable (`HideWhenUnavailable` hides it) | disposing |
| Skill buttons in the details | `Activatable` / `Commands` put the scope's gate in; otherwise the gate denies all | disposing |
| Pick badges, hint | `Numbered` / `Hint` | disposing |

And `Next()` filters too: a gesture the scope didn't allow never comes out of it, even if a view raised it.

Two inputs are always on, by design: **tapping a face-up card shows its details**, and tapping a listing zone shows its
card list. A tap that the scope *does* take (a pick) doesn't open the details (`InputBoard.Takes`).

So the rules for a strategy are:

- **Always `using var scope`** (or dispose it in `finally`). A scope left open leaves its drags and buttons live.
- **One step, one scope.** For a gesture in several steps (drop, then tap tributes), dispose the first scope before
  opening the next, so the hand isn't still draggable while tributes are tapped. Put each step in its own block or
  method:

```csharp
CardDropped drop;
using(var choose = input.Board.Scope().Drag(...).Drop(...).Hint(Text.Play))
{
    drop = (CardDropped)await choose.Next();
}
var fits = offered.Of<NormalSummonRequest>()
    .Where(summon => ReferenceEquals(summon.Monster, drop.Card) && ReferenceEquals(summon.Zone, drop.To)).ToList();
if(fits.Count == 1) return fits[0];
return await PickTributes(fits);   // its own scope: Zones(tributes) + Numbered(picked) + Button(Confirm)
```

### 12.4 Picking on the board: tap, badges, Confirm

For several zones or circles (tributes, Xyz materials, several skill targets), the pattern is a loop of short scopes:
each tap changes the picked list, and a new scope shows the new state.

```csharp
// Sketch: pick between minimum and maximum of the offered zones by tapping, then Confirm
async Task<IReadOnlyList<TZone>> PickZones<TZone>(IReadOnlyList<TZone> offered, int minimum, int maximum, string hint) where TZone : class, ICardSpace
{
    var picked = new List<TZone>();
    while(true)
    {
        using var scope = input.Board.Scope()
            .Zones(offered.Cast<ICardSpace>())
            .Numbered(picked.Cast<ICardSpace>())
            .Hint(hint);
        if(picked.Count >= minimum && picked.Count <= maximum) scope.Button(BoardButton.Confirm);

        switch(await scope.Next())
        {
            case ButtonPressed { Button: BoardButton.Confirm }:
                return picked;
            case ZoneClicked { Zone: TZone zone }:
                // Tap again to drop it; a new pick only while there's room
                if(!Refs.Remove(picked, zone) && picked.Count < maximum) picked.Add(zone);
                break;
        }
    }
}
```

Zones are records, so remove and compare them by reference (`Refs.Remove`, `ReferenceEquals`), never `List.Remove`.
`Refs` lives in `Games/Vanguard/Input/DefaultVanguardAnswers.cs` today: move it to `Games/Shared/` before that file is
deleted.
For a single pick, return on the first tap and leave out Confirm. Cards on the board work the same with
`Cards(zone, allowed)` and `CardClicked`; Counter Blast uses `LongPress` and `CardLongPressed` instead of taps.

### 12.5 Showing a selection panel

When the cards are hidden or piled (deck, soul, graveyard, Extra Deck), pick in a panel instead of on the board. The
panels already exist; a strategy only calls a prompt with the panel and the cards:

| Panel (node in the provider's scene) | Reached as | Prompt | Use |
|---|---|---|---|
| `CardList` (`%CardListComponent`) | `input.EffectPicker` | `Prompts.DragOutOfCardList(list, title, options, canDecline, inspect)` | Pick **one**: a deck search, a skill, the next trigger, a chain response. Press a card to read it, drag it out to pick |
| `SelectFromCardList` (`%SelectFromCardListComponent`) | `input.CardPicker` | `Prompts.PickCards(list, title, selectionTitle, min, max, cards)` | Pick **several**, then Confirm: Soul Blast, a heal, cards for a cost |
| `SelectCardsComponent` (`%SelectCardsComponent`) | `input.HandPicker` | the mulligan strategy's own loop | Drag cards **out of the hand** into the panel, Confirm there |
| `CardListPanel` (`%ChainView`, YuGiOh) | the provider | `Prompts.ShowToInspect(panel, title, items, inspect)` | Read only, beside another prompt |

For example, a deck search asked as `Choose<CardBase>` with one card to pick:

```csharp
var (picked, card) = await Prompts.DragOutOfCardList(input.EffectPicker, question.Prompt,
    question.Offered.Select(card => (card, card)).ToList(),
    canDecline: question.CanCancel,
    inspect: (card, _) => input.ShowDetails(new CardDetail(card)));
return picked ? [ card ] : [];
```

The prompts clean up after themselves (`finally`): the panel closes and its handlers are removed whether the player
picked or declined.

**Adding a new kind of panel:**

1. Design it as a scene and put it in the provider's scene **under the `PromptLayer`**, with a unique name. If it should
   collapse with a swipe down, its script implements `ICollapsiblePrompt` (the layer finds it when ready).
2. In the provider's `_Ready`, `GetNode<YourPanel>("%YourPanel")`, and pass it into `<Game>Input` as a property with a
   one-line summary of what it's for.
3. Write its prompt as a static method in `Prompts` (or on the panel) that shows it, awaits the answer through a
   `TaskCompletionSource`, and in `finally` removes its handlers and hides it, like `PickCards`.
4. Exported values (colours, sizes, texts that aren't the question's prompt) go on the panel's script, not in the
   strategy.

### 12.6 Checking it

Run it in Godot (F5 for a real game). For a new gesture, look at:

- only the cards with an offered request can be lifted, and only the right zone lights up with the right labels;
- letting go anywhere else does nothing, and the card goes back;
- the buttons show only while offered, and the hint says what is asked;
- after the answer, nothing stays draggable, highlighted or numbered (the scope was disposed);
- tapping other face-up cards still shows their details.
