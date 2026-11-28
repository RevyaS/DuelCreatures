# DuelCreatures

A Godot 4.5.1 C# (net9.0, mobile/touch-first) client for card games whose rules live in **ArC**, a separate C#
library. Two games are playable: **Vanguard** (vs trial-deck bots, or an ONNX AI) and **YuGiOh** (vs the Exosister bot).

Sibling repos, checked out next to this one:

| Repo | Path | What it holds |
|------|------|---------------|
| ArC | `../ArC` | Game logic: sessions, play areas, zones, rules, skills, bots, events. Tests in `ArC.CardGames.Tests` (NUnit). |
| Rev-Godot-Core | `../Rev-Godot-Core` | Shared Godot helpers: `IBindable`/`IProvide`/`DependOn`, `Bindings`, `UniqueNodes`, `TypedEventBus`, `InteractionScope`, `NodeExtensions` (`Descendants`, `IgnoreMouse`). Tests in `RevGodotCore.Tests`. |
| rev_godot_core addon | `addons/rev_godot_core` (git submodule) | Godot components such as `GestureDetector`, `DragScrollContainer`, `VBoxNodeContainer`. |

## Current situation (read first)

- **ArC is being rewritten.** This repo's job is to follow the rewrite: update every place that uses ArC's API, and
  keep every input scenario below working exactly as described. The GUI behaviour is the spec; ArC's shape may change.
- **The references are temporary.** `Duel Creatures.csproj` and `../Rev-Godot-Core/RevGodotCore.csproj` use
  `<ProjectReference>` to ArC and Rev-Godot-Core; the original `<Reference>` DLL paths are kept next to them as
  comments. Going back to DLLs is step 9 of the plan (`docs/ArC-Workflow-Plan.md`); keep
  `Compile Remove="RevGodotCore.Tests/**"` in `RevGodotCore.csproj`.
- **Design history and status** are in `docs/ArC-Workflow-Plan.md` (decisions log, steps 1–9 with what each did).
  **How to build a board for a game** is `docs/New-Game-Board-Guide.md`. Update both when the architecture changes.
- **Adopting the rewritten ArC** (four questions, menus of complete requests, gestures matched to offered requests):
  `docs/ArC-Rewrite-Adoption-Guide.md`. Read it before touching the input layer; its section 8 is the migration order.

## Building and testing

```bash
dotnet build "Duel Creatures.csproj" -nologo -v q
```

```bash
dotnet test ../ArC/ArC.CardGames.Tests -nologo
```

- There's no Godot executable in the agent's environment: the user runs scenes in the editor. Say what you built and
  didn't run; give them the scene and what to look at.
- Debug scenes (F6 in Godot): `DebugBoardRunner/VanguardBotSession.tscn`, `DebugBoardRunner/YuGiOhBotSession.tscn`
  (bot vs bot on the real board, with `LayoutWatch` warnings), `DebugBoardRunner/ZoneCheck.tscn` (zone scenes).
  Real games start from the main screen (F5): pick a deck and an opponent, then Play.
- Known ArC test state: `TheOpponentsAshBlossom_NegatesMartha` fails (pre-existing). YuGiOh bot games can hit a
  `SummonXYZ` "needs 1 to choose from, there are 0" now and then (a rules bug, shuffle-dependent).

## How the client is put together

```
ArC session ── CardsMoved / CardTurned / DeckShuffled ──▶ ArcDriver ──▶ zone views (IZoneView) ──▶ Presenter (animations, in order)
            ── typed game events (PhaseStarted, AttackDeclared, ...) ──▶ session components (labels, battle lines, pinned skill)
            ◀── answers ── <Game>InputProvider (IPlayerInput) ◀── strategies (InputScope + gestures) ◀── InputBoard ◀── gestures on views
```

- **Board** (`Games/Shared/Board/`): `ArcDriver` binds zone views to ArC zones **by node name** (unique node `%Name`
  = the zone's property name), fails at startup listing every problem (`BindingException`), turns moves and turns
  into `ZoneChange`s, animates by move reason (`Animate<TReason>`, `Sound<TReason>`, `Hold<TReason>`), checks views
  against zones at every settle in debug builds (desync errors). Zone scenes: `CardSlot`, `CardBox`, `LineZone`,
  `HandZone`, `StackZone` (`Zones/`). Play area scenes have a `PlayAreaView` root; `QuarterTurnContainer.FacesOwner`
  keeps things upright on the far side.
- **Binding:** extensions under a zone view bind to its zone (`IBindable<TZone>`); anything in a play area scene that
  shows the play area binds to it; session components bind via `driver.BindAll<GameSession>`. Context comes from
  ancestors with `DependOn<T>()` (`IProvide<T>`: the session, `PresentationContext`, `ArcDriver`, `IActivationGate`).
- **Input** (`Games/Shared/Input/`): `InputBoard` + `InputScope` (what may be tapped, dragged, dropped, aimed,
  swiped, long pressed, which buttons and details activations are offered, the hint). Gestures are game-term
  records (`CardDropped`, `Aimed`, `ZoneClicked`, …). `Prompts` has the shared prompts (`PickCards`, `Choose`,
  `DragOutOfCardList`, `ShowToInspect`).
- **Per game** (`Games/<Game>/Input/`): `<Game>Input` (context), phase and window strategies that match a gesture to
  one of the offered requests (`Offered`), and `<Game>Choices` (answers `Choose`/`Confirm` by the question's shape).
  The input provider (`Games/<Game>/Board/<Game>InputProvider.cs`) implements ArC's four questions, after
  `Presenter.WhenIdle()`: `ChooseAction` goes to the strategy for `choice.Where` (an unknown one throws), and every
  answer is checked with `Expect`. A question with no gesture throws `UnansweredQuestion`.
- **Input screens** (`Games/Shared/HUD/Prompts/`, `docs/Input-Screens-Plan.md`): a question can open an
  `InputScreen` over the board, in the `PromptLayer`, one at a time. The three shared screens have shared layouts
  (`SwipeUpPicker.tscn`, `CardSelectScreen.tscn`, `FieldSelectScreen.tscn`); each game's is an inherited scene adding
  its card details scene as the skill banner (`Games/<Game>/HUD/Prompts/`). Providers find them with `InputScreens`;
  card picks share `ScreenCardSelection`, skill picks `SkillPicks`. Built from shared part scenes (`SelectableCard`,
  `SelectionGrid`, `PromptHeader`, `PromptButtons`, `DetailsSpace`, `FieldFocus`); games add layouts. The field
  selection screen enlarges the real board (`FieldFocus` scales its root) rather than copying it.
- **HUD:** card details are built from parts (`Games/Shared/HUD/CardDetails/`, per-game scenes); activation buttons are
  gated by `IActivationGate`. Prompt panels sit in a `PromptLayer` (swipe down collapses, swipe up/tap restores) with
  `ResolvingSkillDetails` (the player's own skill pinned while it's activated or resolved).
- **Looks:** `CardGameStyle` resources (`Games/<Game>/<Game>Style.tres`: card scene, art folder, default sleeve,
  sleeve choices), registered by card type in `Games/CardGames.cs`. Card faces: `CardFaceText` labels (unique names)
  bound and filled by the game's `Card` subclass (`FindNodes`, `ShowFace`), in `CardFacePanel`s; no script per text. Art: `res://Assets/Cards/<Game>/<CardNameWithoutSpacesOrPunctuation>.png` (`Assets/` is
  git-ignored). The player's sleeve per game is saved in the profile (`DataManager.SleeveFor/ChooseSleeve`).

## What the client needs from ArC (re-map these in the migration)

The rewrite may rename or reshape these; whatever replaces them must still give the client the same information.

- **Every change to a zone is an event.** Moves through `PlayAreaBase.Move`/`PublishMoved` (`CardsMoved` with a
  move-reason record), in-place changes through `PlayAreaBase.Turn` (`CardTurned`), `DeckShuffled`. Cards leaving for
  no zone use a pseudo-zone (YuGiOh `TokenOrigin`, Vanguard `OutOfGame`). Proven by
  `YuGiOhViewEventCompletenessTests`/`VanguardViewEventCompletenessTests` with the shared `EventOnlyBoard`: port
  these tests first, they tell you whether the board can drift.
- **Zones are public properties** of the play area (`ZonesByName`) and of the board (shared zones: YuGiOh
  `ExtraMonsterZone1/2`, Vanguard `GuardZone`). Scene node names must match the property names.
- **`EventBus.Subscribe<T>` (typed, delivered immediately even during `Hold()`), `EventBus.Settled`.**
- **Events the GUI listens to:** `PhaseStarted` (strategies, phase labels), YuGiOh `AttackDeclared`/`BattleEnded`,
  `SkillStarting`/`SkillFinished` (with `Owner`, `Step`), Vanguard `AttackDeclared`/`BoostDeclared`/`BattleEnded`,
  `SkillStarting`/`SkillFinished`.
- **Move reasons** mapped to animations, sounds and pauses in `VanguardPresentationConfig.ShowVanguardMoves`. A new or
  renamed reason must be mapped again. YuGiOh maps none yet (its moves use `ArcDriver`'s defaults).
- **Session builders** (`YuGiOhSessionBuilder`, `VanguardSessionBuilder`) taking each player's `IPlayerInput` lazily
  (ArC's `PlayerInputs`). The input providers implement `IPlayerInput` (the four questions) and keep their own
  `PlayArea`/`OpponentPlayArea`, set by `Activate`.
- **Rule queries** the strategies filter with, never deciding legality themselves: YuGiOh `YuGiOhGameRules`
  (`CanNormalSummonNow`, `CanSetFromHand`, `CanActivateSpellFromHand`, `Usable…Skills`, `UsableSetCardActivations`,
  `XYZMaterialOptions`, `CanAttack`, `ValidateTarget`, `CanChangeBattlePosition`, `CanFlipSummon`, …); Vanguard
  `VanguardGameRules` (`CanRide`, `CanCallRearguard`, `ValidateSwap`, `HandCardCanActivateSkill`,
  `UnitCircleCanActivateSkill`, `UnitCircleCanAttack`, `AttackingUnitCircleCanBeBoosted`,
  `TargetCircleCanBeTargetted`, …); skill options through `SkillRules.CanChooseSkillOption`. Game states read by
  prompts: `GuardState`, `TriggerPowerState`/`TriggerCriticalState`/`TriggerStandState`,
  `CounterBlastPhaseState`/`CounterChargePhaseState`, `SearchDeckState`, selection states.
- **Skill text interpreters** (`YuGiOhSkillTextInterpreter`, `VanguardSkillToStringInterpreter` incl. `ExtractSkillAction`).

## Input scenarios to keep (the behaviour spec)

Shared, both games:
- Tap a face-up card: its details (unless the tap answers the current prompt). Tap a listing zone (drop zone,
  graveyard, banish, own Extra Deck): its card list; pressing a listed card shows its details.
- Aiming (attacks): no line while searching; only a valid target under the finger shows the line (scale 0.4) and the
  target marker. Letting go anywhere else does nothing.
- Every attack, including the opponent's and bots', stays drawn (line + target marker) from declaration to battle end.
- Skills, skill order, triggers, "choose one of" options (and YuGiOh's chain responses) are picked on the **swipe-up
  picker** (`SwipeUpPicker`, through `SkillPicks`): swipe a card up out of the row onto the space above to pick it, let
  go on the row to keep it there; tap a card for what it stands for; only **Cancel** declines, shown when the question
  may be declined. Options show only the ones ArC says can be chosen.
- Cards picked by a question: on the field, by their zones/circles on the **field selection screen**; anywhere else
  (or several places at once) on the **card selection screen**, tagged with where they are (`VanguardCardTags`,
  `YuGiOhCardTags`). Several: tap to pick (numbered), tap again to drop, **OK** to finish.
- Card details list every skill, an ability card's effects included (`CardDetail.AllSkills`: a spell or trap is its
  effect).
- The player's own skill is pinned while it's activated or resolved; prompts collapse with a swipe down (chevron bar),
  return with a swipe up or tap.
- Next Phase / Confirm buttons appear only while offered (`BoardButtonView.HideWhenUnavailable`); the hint says what
  is asked.

YuGiOh (`YuGiOhMainPhaseStrategy`, `YuGiOhBattlePhaseStrategy`, `YuGiOhResponsesPrompt`, `YuGiOhChoices`):
- Drag a monster from the hand onto a monster zone: the zone splits into **Summon | Set** labels in its top corners.
- Drag a spell onto a spell & trap zone: **Activate | Set**; a trap: **Set** only.
- Swipe a monster left for Defense Position, right for Attack Position (right on a face-down one Flip Summons).
- Effects: tap the card, press the effect in its details; a Set card: press **Activate** in its details. Xyz Summon:
  open your Extra Deck list, press the monster, press **Xyz Summon** in its details, then tap materials (numbered,
  Confirm). A tribute summon: drop the monster on a zone, then tap the tributes (numbered, Confirm).
- Battle: drag from a monster onto the monster to attack; with no opponent monsters, onto the opponent's **hand**
  (direct attack). Next Phase / End Phase buttons.
- Chain responses: swipe the effect up on the swipe-up picker, **Cancel** to pass; the chain so far is shown above the
  prompt (`YuGiOhSwipeUpPicker`'s `%Chain`), tapping a link shows the effect it activated, without answering.
- Searches, costs, materials asked by a question: the card selection screen; zones and field cards asked by a
  question: the field selection screen. Tributes and Xyz materials of a summon you started are still tapped on the
  board (numbered, Confirm).

Vanguard (`VanguardMulliganStrategy`, `VanguardRidePhaseStrategy`, `VanguardMainPhaseStrategy`,
`VanguardAttackPhaseStrategy`, `DefaultVanguardAnswers`, `VanguardGuardPrompt`):
- Mulligan: drag cards from the hand into the selection panel (drag back out to keep), Confirm there.
- Ride: drag a card that can ride onto the vanguard (**Ride**); Next Phase not to ride.
- Main: drag from the hand onto a rear-guard circle to call (**Call**, overcall allowed); drag a rear-guard onto the
  other circle of its column to move it (**Move**, the card follows the finger); [ACT] skills from the card details.
- Attack: drag from a front-row unit to a target; boost by dragging from the back-row unit over the unit in front
  (line to the boosted unit first), then to the target. Targets show three arrows.
- Guard: the **guard screen** (`VanguardGuardScreen`): the battle on top (attacker, its power and critical, against
  your overall power), one grid of the hand cards that can guard (Hand tag, shield shown) and the front rear-guards
  that can intercept (Intercept tag); tap to pick (numbered) and see details; your overall power previews the picked
  shields; **Guard** answers, **No Guard** passes. Nothing moves in ArC until Guard.
- Cards from the soul, drop zone, deck (searches, superior call), damage zone (heal) or hand, or from several places
  at once: the **card selection screen** (`CardSelectScreen`): each card tagged with where it is; a tap picks the card
  (numbered, outlined) and shows its details, a tap again drops it; once the most are picked the others are tinted and
  a tap only shows details; OK once the count fits; Cancel when optional.
- Counter blast / counter charge: the card selection screen with the damage; picked cards show how they'll face
  (counter blast: face down; counter charge: the face-down cards, the picked ones face up); OK when exactly enough.
- Trigger power / critical / stand: the **trigger screen** (`VanguardTriggerSelectScreen`, the field screen with the
  battle and the revealed trigger on top): tap a unit to pick it (numbered), the battle previews what it would do
  (power or critical to the attacker, power to the defender), tap another to move the pick, OK answers.
- Skill targets, units picked by circle: the **field selection screen** (`FieldSelectScreen`) enlarges the
  play area holding the circles (the opponent's cards turned to face you), tap highlighted circles (a tap that selects
  doesn't open details; other taps show details at the top); several are numbered, then OK; Cancel when optional.
  Swipe down to see the whole board.
- Superior call: pick the cards from the deck, then on the field selection screen tap a highlighted circle to place
  the leftmost card of the row there, or drag any row card onto a circle; tap a placed card to take it back; OK.
- Long press a vanguard: its soul list. Drive/damage checks and the opponent's guards pause briefly to be read.
- Layout: the phase shows in the turn player's play area, between the (hidden) extra circles; the hint sits under
  your phase label; Next Phase and Confirm share the place left of the guardian circle.

## Conventions (the user's, keep them)

- **The user designs scenes; the code binds to them.** Find nodes with `GetNode<T>("%Name")` (unique names), never
  `[Export]` node references. No layout enums: layouts are scenes.
- **UI values are exported** (colours, sizes, timings, texts), never magic constants.
- **Card sizes are ratios of the Card component**, never fixed pixel sizes: anything holding cards exports a
  `CardScale` (`CardArea`, `CardLine`, `CardListPanel`, `SelectableCard`, `SelectionGrid`) and sizes itself as
  `SizeConstants.CardDefaultSize * CardScale`, with the card scene scaled by the same ratio. Layouts around cards are
  given in cards too (e.g. `SelectionGrid.VisibleRows`), so changing a scale resizes everything with it.
- **The UI is dumb.** Legality comes from ArC's `Can…`/`Validate…`; strategies only limit which inputs are allowed.
- **Strongly typed, fails loudly**, at build time where possible (typed questions, typed gestures, binding reports).
  Avoid `object`, strings and primitives that can mean anything.
- **Touch first:** gestures (drag, drop with labelled regions, swipe, aim, long press), not action menus.
- ArC's cards and zones are **records**: compare them by reference (`ReferenceEquals`, `Refs.Has/Remove`,
  `ReferenceEqualityComparer`), never with `==`/`Contains`.
- Godot scripts: one class per file named like the class; `[Tool]` scripts must cope with the editor (fields reset
  after a C# rebuild: look nodes up again, as `CardBaseComponent.FindNodes` does).
- Walk subtrees with `node.Descendants()` / `Descendants<T>()` (Rev-Godot-Core), not hand-written recursion.
- Comments and docs: say what something is and why, in plain words; no history in comments.
- **Don't commit.** The user reviews and commits themselves (and excludes the temporary csproj changes). Staging
  deletions with `git rm` is fine; say so.
