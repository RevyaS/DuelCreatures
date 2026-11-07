# ArC ↔ Godot Workflow Plan

Goal: make a new card game playable quickly. ArC writes the logic, Godot scenes are designed by hand, and the
script work left on the Godot side is:
- naming nodes;
- a few typed extensions;
- InputProviderStrategies that limit which inputs are allowed, using each game's `*GameRules`.

This document is the reference to check while implementing. Update the **Status** column as steps land.

---

## 1. Principles

1. **ArC owns all logic.** That covers skills, phase and action flow, and validation. The UI is a dumb projection of
   ArC state plus a gesture source.
2. **Prompts stay plain.** ArC actions keep asking follow-up questions (`SelectCardFromHand`, `SelectOwnZone`, …).
   ArC does not pass candidate lists. Deciding which inputs are legal is the InputProvider's job, using the same
   `*GameRules` (`Validate*` / `Can*`) that ArC uses to validate.
3. **Fail loudly, as early as possible.**
   - At build: typed APIs. During this refactor `<ProjectReference>` also makes ArC changes break the Godot build;
     after it, DLL references return (D16), so rebuild ArC before building Godot.
   - At scene start: the binding validator lists every mismatch.
   - At the moment of the problem: a clear exception, never a silent no-op.
   - Avoid `object`, magic strings and catch-all primitives.
4. **Convention over wiring.** A node whose name matches the ArC property name is bound automatically. Code is only
   needed for the exceptions.
5. **ArC can grow generic pieces early.** A concept used by one game today can live in the ArC base if other games
   are likely to need it later.

---

## 2. Decisions log

| # | Topic | Decision |
|---|---|---|
| D1 | Prompt candidates in ArC | **Rejected.** InputProvider plus `*GameRules` filter inputs. |
| D2 | Legal-move (fully parameterized) actions | **Rejected.** Actions stay intent-style; the UI pre-answers follow-up questions (§6.3). |
| D3 | Action visitor (build error on new action types) | **Deferred.** For now a new action that no strategy maps is silently unplayable. |
| D4 | SessionBuilder in ArC | **Accepted.** |
| D5 | Reusable zone nodes | **Accepted.** One scene per zone kind; no layout enum (§5.1). |
| D6 | `[Export]` node references for binding | **Rejected.** Too much inspector setup. Nodes are bound by unique name (§5.3). |
| D7 | Manual `GetNode("%Name")` inside scene scripts | **Kept.** It's part of designing the scene. |
| D8 | Seat/owner property on zones | **Rejected.** Ownership comes from the bound ArC `PlayArea`. |
| D9 | Facing | A simple bool display default. Zones draw the card's ArC `Facing` and redraw on `CardTurned`. |
| D10 | Turning cards sideways | **Decided by which zone scene is used** (§5.1), not by a flag. |
| D11 | `Inspectable` | **Accepted.** Exported bool on group zones. |
| D12 | Sandbox (rule-free) mode | **Deferred.** All boards are ArC-driven for now. The view layer is designed so a sandbox driver can be added later without changing views (§5.2). |
| D13 | CardDetails | **One per game**, built from reusable parts; activation buttons are gated outside the scene (§7). |
| D14 | Rotatable line zone (resource systems, e.g. MTG lands, Duel Masters mana, OPTCG DON!!) | **Deferred.** It will be a future `RotatableLineZone`. |
| D15 | Chickensoft-style tree binding | Lives in the **Rev-Godot-Core library** (pure C#), not in the addon. |
| D16 | Project references | `<ProjectReference>` **only while this refactor is in progress** (step 1), so ArC and Rev-Godot-Core changes break the Godot build right away. **Revert to DLL `<Reference>`s once everything is done** (step 9). |

---

## 3. Where code lives

| Piece | Location | Reason |
|---|---|---|
| SessionBuilders, `CardTurned`, `IManuallyActivatedSkill`, `ISkillTextInterpreter`, zones exposed by name | **ArC** | Game logic and model |
| Tree binder: contravariant `IBindable<in T>`, unique-name matcher, validator report, provide/depend down the tree (generalizes `IDependentComponent`) | **Rev-Godot-Core library** | Pure C# on GodotSharp, no card types |
| Typed event bus, `InputScope` core (disposable handlers, self-restoring flags, `Next<T>(predicate)`) | **Rev-Godot-Core library** | No card types |
| rev_godot_core addon | Unchanged | Only for scenes and editor components |
| Zone scenes, `IZoneView`, change records, gestures, `ArcDriver<TSession>`, presenting moves from `CardsMoved`, CardDetails parts, input provider base, default prompts | **DuelCreatures `Games/Shared`** | Depends on ArC.CardGames and card scenes |
| Game-specific extensions, CardDetails scenes, strategies | **DuelCreatures `Games/<Game>`** | Per game |

---

## 4. ArC changes

### 4.1 SessionBuilder
- `YuGiOhSessionBuilder` and `VanguardSessionBuilder` build:
  - the event bus;
  - the effect service;
  - the data query (the session/dataQuery cycle is resolved inside the builder);
  - the skill service;
  - the game and the session.
- Each player's input provider comes from a `Func<Services, TInput>`.
- Used by Godot sessions and by the tests.
- Removes the `createdSession = null!` workaround from every session script.

### 4.2 Generic events and markers
- **`CardTurned(PlayAreaBase PlayArea, ICardSpace Space, CardBase Card)`**: a base `IGameEvent`, published by a new
  `PlayAreaBase.Turn(space, card, facing?, orientation?)` helper. Every facing or orientation change goes through it:
  - YuGiOh: set, flip, battle position change, `FlipToActivate`;
  - Vanguard: stand/rest, damage-zone face-down.

  It replaces Vanguard's `CardFaceChanged` and `UnitCircleOrientationChanged`.
- **`IManuallyActivatedSkill : ISkill`**: a marker on skills the player activates by choice:
  - `VanguardActivationSkill`;
  - `YuGiOhIgnitionSkill`, `YuGiOhQuickSkill`;
  - the spell/trap card-activation skills.

  Used by CardDetails to decide which skills get buttons.
- **`ISkillTextInterpreter { string Describe(ISkill skill); }`**:
  - Vanguard's `VanguardSkillToStringInterpreter` implements it.
  - YuGiOh gets a fallback ("Ignition effect", "Quick effect", …) until real text exists.
- **Zones exposed by name**: a public name → `ICardSpace` lookup built from the existing `ZoneDiscovery`, for
  node-name binding.

### 4.3 Moves
- Every move publishes `CardsMoved`. YuGiOh already does this through `PlayAreaBase.Move`; confirm there are no
  bypasses.
- Vanguard-only zone events (`CardAddedToDamageZone`, `CardAddedToDropZone`, `CardAssignedToUnitCircle`, …) are
  replaced by `CardsMoved` with Vanguard move reasons. **This happens during the Vanguard migration (step 8)**, so
  Vanguard keeps working until then.

### 4.4 Known ArC bug: Set spells/traps turned sideways (YuGiOh)
- `YuGiOhPlayArea.SetCard(YuGiOhSpellOrTrapCard …)` sets `to.Orientation = HORIZONTAL`. Real YuGiOh keeps Set
  spells/traps vertical, so this is a bug.
- `FieldZone : SpellAndTrapZone`, so a Set field spell is affected too.
- **Decision:** S/T and field zones are `CardSlot`s (§5.1). The bug is left for later. Until it's fixed, Setting a
  spell/trap will trigger `CardSlot`'s loud orientation error. That's expected, and it's the reminder to fix it.

---

## 5. View layer (Godot)

### 5.1 Zone scenes
Each zone kind is its own scene. Its shape tells you its rules, which also makes space planning predictable.

| Scene | Shape | Turns sideways | Face down | Built from | Used for |
|---|---|---|---|---|---|
| `CardBox` | Square box (fits a sideways card) | **Yes** | Yes | `UnitCircleComponent` | Vanguard unit circles, YuGiOh main and extra monster zones |
| `CardSlot` | One card's shape | **No.** Receiving a horizontal orientation is an error | Yes | `TriggerZoneComponent` / `CardArea` | Vanguard trigger zone, YuGiOh spell & trap zones, field zone |
| `LineZone` | Row of fixed slots | No | Yes | `CardLineStatic` | Vanguard damage zone, guard zone |
| `HandZone` | Dynamic row | No | Per card's facing | `CardLineDynamic` / `HandComponent` | Hands |
| `StackZone` | Pile with count | No | Top card only | `CardVerticalStack` / `DeckComponent` / `DropZoneComponent` | Deck, graveyard/drop zone, soul, banish, extra deck |
| `RotatableLineZone` | Row | Yes | Yes | n/a | **Deferred (D14).** Resource systems |

- Common exports: the existing `FlippedAppearance`, `CardScale` and sleeve settings; the facing bool (D9);
  `Inspectable` on `LineZone` and `StackZone` (D11).
- Extra monster zones are `CardBox`es: monsters there other than Link monsters can be in Defense Position.

### 5.2 Views take change records, not the event bus (replaces `IEventBusUtilizer`)
```csharp
// Games/Shared/Board/ZoneChanges.cs (only CardBase, no game events)
public record CardsArrived(IReadOnlyList<CardBase> Cards, IZoneView? From, CardMove Move);
public record CardsLeft(IReadOnlyList<CardBase> Cards, IZoneView? To);
public record CardTurned(CardBase Card, Facing Facing, Orientation Orientation);

public interface IZoneView : ICardLocation
{
    ICardSpace? Space { get; }                        // set by the binder
    void Apply(CardsArrived change, PresentationContext presentation);
    void Apply(CardsLeft change, PresentationContext presentation);
    void Apply(CardTurned change, PresentationContext presentation);
}
```
- **`ArcDriver`** subscribes **once per session** to `CardsMoved` and `CardTurned`. It looks up the views for
  `From` and `To` (space → view dictionary), maps the `IMoveReason` type to a `CardMove` animation through a registry,
  and calls `Apply`.
  - No per-component bus subscriptions.
  - No `ReferenceEquals` filtering.
  - No game-specific event types in shared views.
- **A future sandbox driver** produces the same records from free drag-and-drop. Views stay unchanged.
- **Removed interfaces:** `IEventBusUtilizer`, `ICardSpaceBindable`, `IPlayAreaBindable`.

### 5.3 Binding by node name
1. **Board level:** the ArC board's properties (`Player1Area`, `Player2Area`) are matched to the board scene's unique
   nodes `%Player1Area` and `%Player2Area`.
2. **Play-area level:** inside each play-area scene instance, each ArC zone property (`MonsterZone1`, `Graveyard`,
   `Deck`, …) is matched to the unique node with that name. `%` names are scoped per scene instance, so both
   players' play areas use the same names.
3. **Shared zones:** a zone held by both play areas (YuGiOh extra monster zones, Vanguard guardian circle) is matched
   at board level.
4. **Exceptions:** one typed line each, e.g. `binder.Bind(PlayerHand, board.Player1Area.Hand);` for hands placed
   outside the play-area scene.
5. **Validator:** throws once at scene start, listing every:
   - ArC zone without a node;
   - zone node without an ArC zone;
   - type mismatch (e.g. a game-specific `IZoneView<DamageZone>` on the wrong zone);
   - extension on the wrong zone kind.

### 5.4 Extensions and session-bound components
**Revised in step 3:** one binding interface from Rev-Godot-Core. Context comes from ancestors, not from a
second `Bind` parameter:
```csharp
// Rev-Godot-Core
public interface IBindable<in TModel> { void Bind(TModel model); }
public interface IProvide<out T> { T Value(); }               // e.g. the session root provides SessionContext
// node.DependOn<SessionContext>() finds the nearest providing ancestor, and fails naming the node if none

// DuelCreatures: what were IZoneExtension<TSpace> and ISessionBound<TSession>
public partial class MonsterStatsExtension : Node, IBindable<MonsterZone> { ... this.DependOn<SessionContext>() ... }
public partial class PhaseIndicator : Control, IBindable<GameSession> { ... }   // shared: any game's session
```
- `Bindings.TryBind` picks the **most specific** `IBindable<T>` accepting the model. Unrelated candidates are reported
  as ambiguous, and a model no interface accepts is reported as a type mismatch.
- **Zone extensions** are child scripts of a zone node:
  - Vanguard power/critical display, ride/call sound effects, boost lines, guard-zone shield display;
  - YuGiOh ATK/DEF display, XYZ material count;
  - shared: count badge (`IBindable<ICardSpace>`).
- **Stats extensions refresh on `EventBus.Settled`.** That replaces the scattered `RecalculateStats()` calls, and
  values from continuous effects can't be missed.
- **Board components** implement `IBindable<TSession>`: phase indicator, life points, attack lines.
  - Written against `GameSession`, they're shared; written against `YuGiOhGameSession`, they're game-specific.
  - They subscribe with `bus.Subscribe<T>()`. `SessionContext` keeps the returned `IDisposable`s and disposes them on
    teardown.
- **Debug desync check:** each zone compares its displayed cards with its ArC `Space` on `Settled` and logs an error
  on mismatch.

---

## 6. Input

### 6.1 Gestures
Zone views and board UI publish typed gesture records into one gesture bus. Gestures carry **ArC zones and cards,
never nodes**:
```csharp
public interface IGesture;
public record CardPressed(CardBase Card, ICardSpace From) : IGesture;
public record CardDropped(CardBase Card, ICardSpace From, ICardSpace To) : IGesture;
public record ZonePressed(ICardSpace Zone) : IGesture;
public record ButtonPressed(BoardButton Button) : IGesture;   // enum of board buttons
public record SkillActivationPressed(CardBase Card, ICardSpace From, ISkill Skill) : IGesture;
```

### 6.2 InputScope
A strategy opens a scope and declares what is allowed, using ArC model objects and `*GameRules`. The binder's
space → view map finds the views. Disposing the scope resets every flag, highlight and handler.
```csharp
public async Task<IMainPhaseAction> RequestMainPhaseAction(List<IMainPhaseAction> actions)
{
    using var scope = Input.Scope();
    scope.Drag(PlayArea.Hand, card => card is YuGiOhNormalMonsterCard m && YuGiOhGameRules.TributesNeeded(m) == 0);
    scope.Drop(PlayArea.MonsterZones, zone => zone.IsEmpty);
    scope.Activatable((card, from, skill) =>
        from is IYuGiOhZone zone && skill is YuGiOhNonPassiveSkill s && YuGiOhGameRules.CanActivateSkill(Player, zone, s));
    scope.Button(BoardButton.EndPhase);

    return await scope.Next() switch
    {
        CardDropped(var card, _, MonsterZone zone) =>
            Answer(actions.FirstOf<NormalSummonMonster>()).With<SelectCardFromHandQ>(card).With<SelectOwnZoneQ>(zone),
        SkillActivationPressed(_, IYuGiOhZone zone, YuGiOhNonPassiveSkill skill) =>
            Answer(actions.FirstOf<ActivateEffectsOnField>()).With<SelectOwnZoneQ>(zone).With<SelectSkillToActivateQ>(skill),
        ButtonPressed(BoardButton.EndPhase) => actions.FirstOf<EndMainPhase>(),
    };
}
```
- Asking for a zone that has no bound view throws.
- `Next()` only returns gestures the scope allowed.

### 6.3 Typed pre-answers (replace strategy stash fields)
```csharp
public interface IQuestion<TAnswer>;
public sealed record SelectCardFromHandQ : IQuestion<CardBase>;
public sealed record SelectOwnZoneQ      : IQuestion<IYuGiOhZone>;
public sealed record SelectSkillToActivateQ : IQuestion<YuGiOhNonPassiveSkill>;
// .With<TQ>(answer) compiles only when answer matches TQ's answer type
```
- The input provider base handles each ArC question in this order:
  1. use a pre-answer if there is one;
  2. otherwise walk the **strategy stack from the top** for a strategy implementing the question's interface;
  3. otherwise throw, naming the question and the stack.
- When the action finishes, an unused pre-answer **throws**. This catches ArC flow changes immediately.

### 6.4 Default strategy
- Sits at the bottom of the stack and handles every question with plain UI:
  - an action list;
  - highlight-and-click for zones;
  - a card list for searches;
  - `ChoiceDialog` for yes/no;
  - CardDetails buttons for activations.
- A game is playable as soon as it is bound. Nicer gesture-based strategies replace it one phase at a time.

### 6.5 Inspecting
- Inspecting a card (a click or long press that no active scope uses) is handled by the **driver**, not by
  strategies. It always works.
- Activation buttons are enabled only through the current scope (§7.2).

---

## 7. CardDetails

### 7.1 One scene per game, built from parts
- **Godot inherited scenes:** you *can* change inherited nodes' properties and add new nodes, and you can switch the
  root script to a subclass. You *cannot* delete or reparent inherited nodes; hide them instead.
- So both routes work:
  - a shared `CardDetailsBase.tscn` with game scenes inheriting from it; or
  - fully separate game scenes assembled from reusable parts.

  **Recommendation:** separate scenes per game, assembled from parts. This avoids fighting nodes you can't delete.
- **Parts** are small scenes/scripts, each bound to **one typed card property**:
  ```csharp
  public interface ICardDetailPart { void Show(CardBase card, ICardSpace? from); }

  // Non-generic Godot scripts deriving from a generic base (allowed in Godot 4 C#)
  public abstract partial class CardValuePart<TCard> : LabelValueContainer, ICardDetailPart where TCard : CardBase
  {
      protected abstract string Format(TCard card);
      public void Show(CardBase card, ICardSpace? from)
      {
          Visible = card is TCard;
          if(card is TCard typed) Value = Format(typed);
      }
  }
  public partial class YuGiOhAttackPart : CardValuePart<YuGiOhMonsterCard> { protected override string Format(YuGiOhMonsterCard c) => c.Attack.ToString(); }
  public partial class VanguardPowerPart : CardValuePart<VanguardCard>   { protected override string Format(VanguardCard c) => c.Power.ToString(); }
  ```
- **Shared parts:** card name, skill text (through `ISkillTextInterpreter`), granted skills, activation list.
- **Game parts:** stats and type rows.
- The `CardDetails` root script finds every `ICardDetailPart` underneath it and calls `Show`. There's no per-field
  wiring code.
- A part for the wrong card type hides itself, so a YuGiOh monster-only part on a spell simply disappears.

### 7.2 Activation buttons gated from outside the scene
- `SkillActivationList` is a shared part. It renders one button per `IManuallyActivatedSkill`, base and granted.
- **It never decides whether a button is enabled.** It asks an injected gate:
  ```csharp
  public interface IActivationGate
  {
      bool CanActivate(CardBase card, ICardSpace from, ISkill skill);
      event Action? Changed;
  }
  ```
  - The current `InputScope` provides the gate through `scope.Activatable(...)`.
  - With no scope (or in the deck builder) the default gate denies everything, which replaces `ReadOnlyMode`.
- When the scope changes or closes, `Changed` fires and an open panel re-evaluates its buttons. A button left over
  from an earlier prompt can't fire.
- Pressing a button publishes `SkillActivationPressed`, a gesture like any other (§6.1). The panel needs no board or
  input provider.
- ArC's `ValidateSkillActivation` remains the backstop and throws loudly if a strategy predicate is wrong.

---

## 8. Implementation steps

| # | Step | Done when | Status |
|---|---|---|---|
| 1 | **Build plumbing (temporary, D16):** `<ProjectReference>` from DuelCreatures to ArC and Rev-Godot-Core, and from Rev-Godot-Core to ArC.Common, in place of the DLL references. Keep a copy of the original `<Reference>` items for step 9 | Changing an ArC public API breaks the Godot build | ☑ The original references are kept as comments in both csproj files |
| 2 | **ArC generic additions:** SessionBuilders (§4.1); `CardTurned` + `Turn` helper, `IManuallyActivatedSkill`, `ISkillTextInterpreter` + YuGiOh fallback, zones by name (§4.2); settle §4.4 | ArC tests pass; YuGiOh/Vanguard sessions are built by the builders | ☑ In-place YuGiOh changes now go through `Turn`: battle position change, Flip Summon, flipped when attacked, `FlipToActivate`. Covered by `YuGiOhViewSupportTests`. The Godot factories are replaced by builder lambdas plus `VanguardOpponents`. `TheOpponentsAshBlossom_NegatesMartha` was already failing before these changes. §4.4 stays deferred by decision |
| 3 | **Rev-Godot-Core:** tree binder + name matcher + validator report; typed bus + `InputScope` core | Unit-testable without card types | ☑ Adds `IBindable<in T>`, `IProvide<out T>`, `Bindings` (match by name, most-specific bind, `BindingReport` / `BindingException`), `UniqueNodes`, `TreeDependencies.DependOn<T>()`, `TypedEventBus<TBase>` and `InteractionScope`. 19 tests in `RevGodotCore.Tests` (excluded from the Godot build). The two binding interfaces became one (§5.4) |
| 4 | **Shared view layer:** `IZoneView` + change records; `CardBox`, `CardSlot`, `LineZone`, `HandZone`, `StackZone` from the existing scenes; extensions and board components as `IBindable<T>` + `DependOn`; desync check | Zones draw a scripted sequence of change records correctly | ☑ `DebugBoardRunner/ZoneCheck.tscn` passed in Godot. The code is in `Games/Shared/Board/`: `ZoneChanges.cs` (`ZoneSnapshot` + `ZoneCardsArrived/Left/Turned`, each carrying the zone's content after the change), `IZoneView.cs` (+ `ZoneDesync`, `IShowsPartOfZone`), `ZonePresentation.cs`, and `Zones/` (5 scripts + scenes inheriting the existing ones). Single-card zones bind `ISingleSpace` and group zones bind `GroupSpace`, so a wrong pairing is reported at bind time. Extensions hook `IZoneView.Presented`. Not covered yet: deck shuffle animation (no `ZoneChange` for it; step 5) |
| 5 | **`ArcDriver<TSession>`:** ArC events → change records, reason → animation registry, name binding + validator, inspect gesture. Rebuild the YuGiOh board on it (fixes `YuGiOhBoard` presentation bugs) | The YuGiOh opening draw and the bot's turn render with no game-specific binding code | ☑ Confirmed in Godot with `DebugBoardRunner/YuGiOhBotSession.tscn`. `Games/Shared/Board/ArcDriver.cs` is not generic: it binds a `TwoPlayerBoard<TPlayArea>`. `Bind` (views outside the convention, e.g. hands) → `BindBoard` (by name; hidden zones as a typed lambda) → `Start` (throws `BindingException` listing everything). Zone-view children are bound as extensions; `BindAll<TModel>` binds board components (e.g. the new shared `PhaseLabel`). Move animations come from the `IMoveReason` type (`Animate<TReason>`). Adds `ZoneShuffled`. Debug builds check every view at each settle, in presentation order. The YuGiOh board is rebuilt from zone scenes named after the ArC properties; `YuGiOhBoard.cs` binds only the two hands. Bot vs bot lives in its own debug scene, `DebugBoardRunner/YuGiOhBotSession.tscn`; both sessions run through `SessionLoop.Run`, which reports game exceptions. In ArC, `YuGiOhViewEventCompletenessTests` (5 bot games) shows `CardsMoved` + `CardTurned` alone reproduce every zone; it found and fixed a bot crash on spells with no card activation (Carpedivem). After the first Godot run: hands and line zones now keep a card's height when empty, so the board no longer jumps on the opening draw; a view that can't show a change now logs an error instead of throwing into ArC (that was what stopped the bots, at the sideways Set from §4.4); missing card art is skipped quietly. Life points are shown by `LifePointsLabel` (refreshed on settle; ArC has no life point event); `TurnLabel` and `PhaseLabel` are separate. The play-area skew came from a long phase label widening the HUD (fixed in the scene). `LayoutWatch` (debug runner) warns when anything in a play area changes size. |
| 6 | **CardDetails:** shared parts, `IActivationGate`, `YuGiOhCardDetails` and `VanguardCardDetails` scenes; the deck builder uses the default (deny) gate | Inspecting works in game and in the deck builder; buttons follow the scope | ☑ Written and building. Shared code is in `Games/Shared/HUD/CardDetails/`: `CardDetail` (card, zone, granted skills), `ICardDetailPart`, `CardDetailsView` (root: shows every part, provides the game's `ISkillTextInterpreter`), and parts `CardNamePart`, `CardValuePart` (a `LabelValueContainer`: label, font and layout stay exported; `For<TCard>` reads one typed property), `SkillTextPart`, `GrantedSkillsPart`, `ActivationListPart`. `IActivationGate` has `CanActivate`, `Activate` and `Changed`; with no provider it is `ActivationGates.DenyAll`, and `ActivationGateSlot.Use(gate)` lets a strategy put its gate in for its scope. Scenes: `Games/YuGiOh/HUD/CardDetails/YuGiOhCardDetails.tscn` (Level/Rank, ATK, DEF, type) and `Games/Vanguard/HUD/CardDetailParts/VanguardCardDetails.tscn` (grade, power, critical, shield, unit type, skill icon). Zones raise `CardInspected` for face-up cards only, and the driver relays it. YuGiOh's HUD shows details for board cards and for cards in the zone list; its `Activations` slot denies everything until step 7. The deck builder uses `VanguardCardDetails` (no gate = read-only). Vanguard's in-game `CardInfo`/old `CardDetails` stay until step 8. Confirmed in Godot (game details, deck builder read-only = activated skills as text) |
| 7 | **Input provider base:** strategy-stack fallback, typed pre-answers, `InputScope` that finds views from ArC zones, default prompts; YuGiOh main-phase strategy including activation from CardDetails | A full YuGiOh game is playable against the Exosister bot | ☑ Played in Godot; remaining polish deferred. **ArC:** the bot's legality checks moved to `YuGiOhGameRules.Choices.cs` (`CanNormalSummonNow`, `CanSetFromHand`, `Usable…Skills`, `UsableSetCardActivations`, `CanSummonFromExtraDeck`, `XYZMaterialOptions`); the bot delegates to them. **Shared input** (`Games/Shared/Input/`): `IGesture` records (`CardClicked`, `ZoneClicked`, `ButtonPressed`, `SkillActivated`), `BoardButton` + `BoardButtonView` (exported kind on any HUD button), `InputHintLabel`, `Question<T>` + `PreAnswers` (unused answers throw at the next action), `Narrowing` (one-shot filter for a question's next prompt), `InputBoard` and `InputScope` (`Cards`, `Zones`, `Button`, `Activatable`, `Hint`, `Next`; highlights and button states undo on dispose), and `Prompts` (`Choose`, `PickCards`). Zones raise `Clicked` and take `SetHighlight` (colours and width exported); the driver relays clicks and inspects what the viewer may see (face-up, or your own cards outside the deck and hand). **YuGiOh** (`Games/YuGiOh/Input/`): `YuGiOhQuestions`, `IYuGiOhAnswers`, `YuGiOhStrategy` (each question falls through to `Below`), `DefaultYuGiOhAnswers` (every question; filtered by rules and selection states), `YuGiOhMainPhaseStrategy` (hand clicks, summon zone, details activation, Next/End Phase, Actions list). `YuGiOhInputProvider` asks: pre-answer, then the phase strategy, then the defaults, after the board is idle. HUD: Next Phase/End Phase are `BoardButtonView`s; Actions, Confirm and a hint label were added. **Touch redesign (mobile):** Actions/Confirm removed. Reusable gestures any game can use: `DropChoicesOverlay` (a zone splits into labelled regions while a card hovers; styled by exports, and a zone uses one placed in its scene), swipes and aims on single-card zones (`GestureDetector` on the zone, card input muted, like Vanguard's unit circles), `AimIndicatorLayer` (an AttackIndicator line that snaps to accepted targets), `CardCommand` buttons in the details (`CardCommandsPart`). `InputScope` gained `Drag`, `Drop`, `Swipe`, `Aim` and `Commands`; gestures gained `CardDropped`, `ZoneSwiped`, `Aimed` (`ZoneTarget`/`AreaTarget`) and `CommandPressed`. YuGiOh: drag to a monster zone (Summon|Set) or a spell & trap zone (Activate|Set, trap: Set); swipe left/right for position (right flips a Set monster); Xyz Summon from your own Extra Deck's list (opponent's stays closed); effects from the details; battle by dragging an attacker onto a target, or onto the opponent's side for a direct attack (`YuGiOhBattlePhaseStrategy`). Addon: `GestureDetector` gained `VerticalSwipes`, and a slow swipe start turns into a drag. Since then: drop labels sit above the zone at its corners; `SelectFromCardList` presses show details; each game's details decide which effects get buttons (`ISkillButtons`; YuGiOh: a card activation only while Set); `CardList.LetThrough(control)` keeps the details (and the chain view) clickable while a list is open; chain responses show the chain so far in a `ChainView` (`Prompts.ShowToInspect`), each card showing the effect its link activated; a card dragged out of a pick list and let go back over the list returns instead of being picked (`CardList` takes it back on its panel, for YuGiOh and Vanguard alike) |
| 8 | **Vanguard migration:** extensions (power/critical, sound effects, boost and attack lines, guard shields); Vanguard zone events → `CardsMoved`/`CardTurned`; strategies ported to `InputScope` | Vanguard plays as before; `VanguardBoard` event relays removed | ◐ Split into sub-steps below |

**Step 8 sub-steps:**

| # | What | Done when | Status |
|---|------|-----------|--------|
| 8a | **ArC:** every Vanguard change a view needs goes through `CardsMoved`/`CardTurned` | `VanguardViewEventCompletenessTests` (5 bot games, all four trial decks) pass | ☑ Moves were already complete. Standing and resting now go through `VanguardPlayArea.Stand`/`Rest`/`StandAll`, which use `Turn` (attack, boost, rest-as-cost, stand trigger, the `Stand` skill action, stand phase). `UnitCircle.Stand`/`Rest` are internal, for test setup only. The rear-guard swap publishes two `CardsMoved` with the new `Swapped` reason, then each unit's orientation. Counter blast and counter charge moved to `VanguardPlayArea` (async) and publish `CardTurned`. The old Vanguard events still fire, so the current Godot board keeps working. The YuGiOh test's `EventOnlyBoard` is now a shared test helper |
| 8b | **Board on `ArcDriver`:** zone scenes named after the ArC properties (`Vanguard` and rear-guards: `CardBox`, rest = sideways; `TriggerZone`: `CardSlot`; `DamageZone`, guardian circle: `LineZone`; `Deck`, `DropZone`, `Soul`: `StackZone`); hands bound as for YuGiOh; move animations and sounds from Vanguard move reasons (`Animate<TReason>`); a bot-vs-bot debug scene | A Vanguard bot game renders with no Vanguard event relays | ◐ Written and building; not yet run in Godot. Built beside the old board, so Vanguard stays playable until 8d switches the session over: `Games/Vanguard/Board/VanguardBoardView.tscn/.cs` (from the old board's layout; `%Player1Area`/`%Player2Area`, shared `%GuardZone` as a `LineZone`, `HandZone` hands, the opponent's hidden) and `VanguardPlayAreaView.tscn` (rear-guards and vanguard as `UnitCircleZone`, a `CardBox` drawn over the circle art; `TriggerZone`: `CardSlot`; `DamageZone`: `LineZone`; `Deck`: hidden `StackZone`; `DropZone`: inspectable `StackZone`). The play area's root script `VanguardPlayAreaView` only draws: sleeve, and the far side's flipped icons and turned deck/drop zone. The soul is a hidden zone for now (listed from the vanguard in 8d); the four extra circles are decoration, not zones. `ArcDriver` gained `Sound<TReason>(cue)`, played once the moved cards arrive; `VanguardPresentationConfig.ShowVanguardMoves` maps every Vanguard move reason to its animation and the ride/call sounds. `CardArea` gained an `AppearanceChanged` hook for subclasses. Debug scene: `DebugBoardRunner/VanguardBotSession.tscn` (Royal Paladin bot vs Kagero bot, with `LayoutWatch`) |
| 8c | **Extensions:** power/critical on circles and shield on guardians (`IBindable<UnitCircle>`, refreshed on `Settled`); attack and boost lines (board component on `AttackDeclared`/`BoostDeclared`, cleared when the battle ends); phase and turn labels shared with YuGiOh | The bot game shows stats, lines and phases as the old board did | ☐ |
| 8d | **Input:** Vanguard questions, answers and strategies on `InputScope` + the shared gestures (ride and call by dragging from the hand, attack by aiming, guard by dragging to the guardian circle, swap, skills through `DragOutOfCardList`, mulligan and damage choices through `PickCards`); `VanguardCardDetails` in game with its activation gate | A full Vanguard game is playable against a trial deck bot, as before | ☐ |
| 8e | **Clean-up:** remove `VanguardBoard`'s relays, `IEventBusUtilizer`, the old Vanguard zone components, `CardInfo` and the old `CardDetails` | Nothing references the old classes; the deck builder and game still work | ☐ |
| 9 | **Revert to DLL references (D16):** swap the `<ProjectReference>`s from step 1 back to `<Reference>` DLL paths in `Duel Creatures.csproj` and `RevGodotCore.csproj`, including the new Rev-Godot-Core output. Fix the net8/net9 output-path mix while there. Keep the `Compile Remove="RevGodotCore.Tests/**"` item in `RevGodotCore.csproj`; it isn't temporary | DuelCreatures builds from freshly built ArC and Rev-Godot-Core DLLs; no `<ProjectReference>` left | ☐ |

**Deferred:**
- the sandbox driver (plugs into §5.2);
- the action visitor (D3);
- `RotatableLineZone` (D14).

---

## 9. Known issues found while scanning
- ~~`YuGiOhBoard.ApplySession` assigns `playerPresentation` twice, so `oppPresentation` stays null, and
  `SetupEventBus` is never called.~~ Gone with step 5 (the board now uses `ArcDriver`).
- `YuGiOhMainPhaseStrategy` / `YuGiOhResponseStrategy` await `TaskCompletionSource`s that nothing ever completes, so
  the game hangs on the human's first prompt. Replaced in step 7; until then watch games with `DebugBoardRunner/YuGiOhBotSession.tscn`.
- ~~The YuGiOh game loop has `Presenter.WhenIdle()` commented out.~~ Restored in step 5.
- ~~The YuGiOh bot crashed activating a spell with no card activation (`First` in `RequestMainPhaseAction`).~~ Fixed in
  step 5 (`FirstOrDefault`; the action just places the spell).
- `YuGiOhFieldComponent.tscn` is no longer used by the board (replaced by `CardBox`); delete it when you're happy with
  the new board.
- ArC: Set spells/traps are turned sideways (§4.4). Left for later; `CardSlot` errors will point at it.
