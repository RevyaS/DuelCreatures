# Building a board for a new game on ArC

How to show an ArC game in Godot and let a human play it, using what YuGiOh and Vanguard use today. Each step names
the files to copy from. The last section lists what is redundant or duplicated, found while writing this.

What you write per game, and what is shared:

| Layer | You write (per game) | Shared (already there) |
|-------|----------------------|------------------------|
| ArC | the game: zones as properties, moves through `Move`/`PublishMoved`, in-place changes through `Turn`, move reasons, rules (`Can…`/`Validate…`), a `…SessionBuilder` | `CardsMoved`, `CardTurned`, `PlayAreaBase.ZonesByName`, `EventBus.Settled`, `PhaseStarted` |
| Look | a style resource (card scene, art folder, sleeves), face parts, move timings and sounds, which move reason animates how | `CardGameStyle`, `CardStyles`, `Card` + `CardFaceText`/`CardFacePanel`, `PresentationConfig`, `Presenter`, `CardFlightLayer`, `SfxBus`/`SfxPlayer` |
| Board | a play area scene, a board scene, a small board script | zone scenes, `ArcDriver` (binding, moves, desync check), `DropChoicesOverlay` |
| Extras | game-specific extensions (stats, shields, battle lines, life points) | `PhaseLabel`, `TurnLabel`, `ZoneLinesLayer`, `IBindable`/`DependOn` |
| Details | a details scene from parts, maybe a stat part or two | `CardDetailsView` + parts, `IActivationGate`, `CardDetailsOverlay` |
| Input | questions, answers interface, default answers, phase strategies, input provider, HUD scene | `InputBoard`, `InputScope`, gestures, `Prompts`, `PreAnswers`, `Narrowing` |
| Session | the session scene script | `SessionLoop` |

---

## 0. ArC: what the game must give the view

The board only ever sees two events: `CardsMoved` and `CardTurned`. Everything below depends on these being complete.

- **Zones are public properties.** Each player's zones are properties of the play area (`ZonesByName` finds them by
  reflection). Zones both players share are properties of the board (`YuGiOhBoard.ExtraMonsterZone1`,
  `VanguardBoard.GuardZone`). Zones held by other zones (XYZ materials, the soul) go in `NestedZones` or are
  listed as hidden when binding (step 3).
- **Every move goes through `PlayAreaBase.Move`** (or `PublishMoved` for moves it can't express, like a ride or a
  swap), with a **move reason record** per kind of move (`YuGiOhMoveReasons.cs`, `VanguardMoveReasons.cs`).
- **Every in-place change goes through `PlayAreaBase.Turn`** (face up/down, rest/stand, Defense Position). Never set
  `Orientation` or `Facing` directly in game code.
- **Rules are queries.** Every `Validate…` has a `Can…`, and anything the UI must offer has a query listing it
  (`YuGiOhGameRules.Choices.cs`: `UsableHandSkills`, `CanNormalSummonNow`, …). Prompts filter with these; they
  never decide legality themselves.
- **A session builder** (`YuGiOhSessionBuilder`, `VanguardSessionBuilder`): `Player1(profile, services => input)`.
- **Prove it:** copy `ArC.CardGames.Tests/VanguardTests/VanguardViewEventCompletenessTests.cs`. It plays bot games
  and rebuilds every zone from the two events alone (`EventOnlyBoard`). That test passing means the board can't
  drift.

## 1. Look: cards, sleeves, moves, sounds

**The game's style** (copy `Games/YuGiOh/YuGiOhStyle.tres`), one `CardGameStyle` resource edited in the inspector:

- `Id`: the game's key in a profile (keep it once players have profiles);
- `CardScene`: how a card looks in game (empty: the base card, art only);
- `ArtFolder`: where the art is, one `.png` per card named after it without spaces and punctuation
  (`res://Assets/Cards/<Game>/BlasterBlade.png`);
- `DefaultSleeve`, and `Sleeves`: the others a player may pick (sleeves are `ColorSleeveInfo` or
  `TextureSleeveInfo` resources, e.g. in `Games/<Game>/Sleeves/`).

Then add the game in `Games/CardGames.cs`: `CardStyles.Register<MyCard>("res://Games/MyGame/MyGameStyle.tres")`.
Every card of that type (and its subtypes) then uses that scene, folder and sleeves.

**The card scene** (copy `Games/YuGiOh/Cards/YuGiOhCard.tscn`): inherit `Games/Shared/Cards/Card.tscn` and place
face parts anywhere under it. A face part is a `CardFaceText` subclass (a Label: write `Format(card)`, null hides it;
`Prefix`/`Suffix` around the value), like `YuGiOhLevelFace`, `YuGiOhAttackFace` and `YuGiOhDefenseFace`. Put
them in a `CardFacePanel` to hide their background when none has a value (a spell's ATK bar). Parts show only face
up. The card needs no script of its own; a card with values that change in game (Vanguard's power) has one
(`VanguardCardComponent`).

**Sleeves on a board:** the board's `Show(session, presenter, sleeves)` calls `driver.UseSleeves(sleeves)`. The
session passes `DataManager.BoardSleevesFor<MyCard>()` (the player's pick, the default for the opponent); a debug
scene passes `BoardSleeves.Defaults<MyCard>()`. A player picks with `DataManager.ChooseSleeve<MyCard>(sleeve)`
(one of the style's `SleeveChoices`), saved in their profile.

**Moves and sounds** (copy `Games/Vanguard/VanguardPresentationConfig.cs`):

- `Register()`: `PresentationConfig.Register(Moves, SfxStreams)`.
- `CardMove`s with their `Transition`s, and `SfxCue`s with their streams.
- An extension like `ShowVanguardMoves(this ArcDriver driver)`: `driver.Animate<Called>(Moves.ToUnitCircle)
  .Sound<Called>(Sfx.Call)` for each move reason, and `.Hold<DriveChecked>(seconds, sides)` for moves to read before
  the game goes on. The most specific registered reason type wins. Moves to the hand and the deck have defaults.

## 2. Play area scene

Copy `Games/YuGiOh/Board/YuGiOhPlayAreaComponent.tscn` or `Games/Vanguard/Board/VanguardPlayAreaView.tscn`.

- **One unique node (`%Name`) per zone property, named exactly like the property.** A misspelt name or a missing
  node fails at startup with a list of every problem (`BindingException`).
- **Pick the zone scene by what the zone is** (`Games/Shared/Board/Zones/`):

| Zone | Scene | Notes |
|------|-------|-------|
| One card, never sideways (trigger zone, spell & trap zone) | `CardSlot` | Sideways is a loud error, on purpose |
| One card that can lie sideways (unit circle, monster zone) | `CardBox` | Square, so resting doesn't change the layout |
| A row (damage zone, guardian circle, banish row) | `LineZone` | `MaxCards` slots; `Inspectable` lists it on press |
| A hand | `HandZone` | `HideCards` for the opponent's; usually on the board, not in the area |
| A pile (deck, drop zone, graveyard) | `StackZone` | `HideCards` for a deck, `ShowCount`, `Inspectable` |

- **A zone that needs its own look** subclasses one of these (`UnitCircleZone : CardBox`, circle art under the card).
- **Turn things that must read upright on the far side** with a `QuarterTurnContainer`.
- **A root script is optional and only draws** (`VanguardPlayAreaView`: sleeve, flipped icons). YuGiOh's
  (`YuGiOhPlayAreaComponent`) is empty.

## 3. Board scene and script

Copy `Games/YuGiOh/YuGiOhBoard.tscn/.cs` or `Games/Vanguard/Board/VanguardBoardView.tscn/.cs`.

- **Nodes:**
  - `%Player1Area` and `%Player2Area`, the play area scene twice; turn player 2's area 180° with a pivot.
  - Shared zones as unique nodes named after the board's properties (`%GuardZone`).
  - `%PlayerHand` and `%OppHand`.
  - `%SfxPlayer`.
- **Script:** one method, called once the game has started:

```csharp
public ArcDriver Show(MyGameSession session, Presenter presenter)
{
    var board = session.Game.Board;
    var driver = new ArcDriver(session.EventBus, presenter, Sfx);
    driver.Bind(PlayerHand, board.Player1Area.Hand, BoardSide.Player);   // views outside the naming convention
    driver.Bind(OppHand, board.Player2Area.Hand, BoardSide.Opponent);
    driver.BindBoard(this, board, area => [ area.Soul ]);               // the rest by name; zones nothing shows
    driver.BindAll<GameSession>(this, session);                          // session components (step 4)
    driver.ShowMyGameMoves();                                            // step 1
    driver.Start();                                                      // fails listing every binding problem
    return driver;
}
```

- **What it can provide its components** (`IProvide<T>`, found with `DependOn<T>()`): the session, a
  `PresentationContext`, the `ArcDriver`. Vanguard's board provides all three; YuGiOh's input provider provides the
  `PresentationContext` instead (see the redundancy notes).

## 4. Extensions and components

These are scripts placed in the scenes. Binding finds them; nothing wires them by hand.

| Kind | Where | Implements | Example |
|------|-------|-----------|---------|
| Zone extension | Child of a zone view | `IBindable<TZone>` | `UnitStats` (power/critical), `GuardianShields` |
| Play area component | The play area root | `IBindable<TPlayArea>` | (YuGiOh's `LifePointsLabel` is bound by hand in `YuGiOhBoard`) |
| Session component | Anywhere under the board or HUD | `IBindable<GameSession>` or `IBindable<MySession>` | `PhaseLabel`, `TurnLabel`, `VanguardBattleLines` |

- Values with no event of their own (stats with continuous effects, life points) are read on `EventBus.Settled`,
  then shown through `presentation.Present(…)`, so they stay in step with the animations.
- Lines between zones: subclass `ZoneLinesLayer` and draw on your game's events (`VanguardBattleLines`).
- `PhaseLabel.OnlyOnItsAreasTurn` inside a play area shows the phase only on the side whose turn it is.

## 5. Watch it before playing it

Copy `DebugBoardRunner/VanguardBotSession.tscn/.cs`: two bots play on your board with F6.

- Errors such as `ArcDriver: <zone> doesn't match its zone` come from the debug desync check, which compares every
  view with the game at each settle.
- `LayoutWatch` warns when anything in a play area changes size.

## 6. Card details

Copy `Games/YuGiOh/HUD/CardDetails/` or `Games/Vanguard/HUD/CardDetailParts/`.

- A `CardDetailsView` subclass that sets the skill text interpreter, and optionally `HasButton` (which skills get an
  activation button where the card is now).
- A scene built from parts: `CardNamePart`, `CardValuePart` (one typed property each), `SkillTextPart`,
  `GrantedSkillsPart`, `ActivationListPart`, `CardCommandsPart`; scroll the skills with a `DragScrollContainer` to
  keep the height fixed.
- Put it in the HUD (YuGiOh), or over the board in a `CardDetailsOverlay` (Vanguard, whose board fills the screen).
  With no activation gate above it, skills show as text (the deck builder).

## 7. Input

Copy `Games/Vanguard/Input/` (or `Games/YuGiOh/Input/`) and the input provider.

1. **`MyQuestions`**: one `Question<TAnswer>` per method of the game's `I…PlayerInputProvider`.
2. **`IMyAnswers` + `MyStrategy`**: the same methods again; the strategy passes each one down to `Below`.
3. **`MyInput`**: what strategies use (player, game states, `InputBoard`, pickers, `PreAnswers`, `Narrowing`).
4. **`DefaultMyAnswers`**: answers every question with plain prompts (`Prompts.PickCards`, `Prompts.Choose`,
   `Prompts.DragOutOfCardList`, `InputScope.Zones` taps). The game is playable once this compiles.
5. **Phase strategies** answer by gestures, opening an `InputScope` per question:
   - `Drag`/`Drop` with `DropChoice`s (labelled regions on the zone);
   - `Aim`, or `Aim(…, via, …)` for a boost (no line until a target is under the pointer; the target shows
     `SetTargeted`: a yellow block on a `CardSlot`, or a zone's own marks by overriding `ShowTargeted`);
   - `Swipe`, `LongPress`, `Cards`/`Zones` taps;
   - `Button(BoardButton.…)`;
   - `Activatable`/`Commands` for the card details;
   - `Hint`.

   Then:
   - Pre-answer the questions the chosen action asks next (`Input.Answers.Set(…)`); any left unasked fail loudly at
     the next action.
   - Use `Narrowing` when a later prompt should only offer what the action can use.
   - Preview answers on the board (guards, a mulligan) and `Resync()` the views afterwards.
6. **The input provider** (`VanguardInputProvider.cs`):
   - implements the game's `I…PlayerInputProvider`, each method as `Ask(question, answers => …)`;
   - swaps strategies on `PhaseStarted` (`EventBus.Subscribe<PhaseStarted>`, published before each phase runs);
   - attaches the `InputBoard` to the driver;
   - handles inspecting (details on tap unless the tap answers something, zone lists, the soul on long press).
7. **The HUD scene:**
   - **Required:** a `CardFlightLayer` (`%CardFlights`).
   - **For the strategies:** an `AimIndicatorLayer`, a `BoardButtonView` per `BoardButton` they offer (set
     `HideWhenUnavailable` to share a spot), and an `InputHintLabel` (Vanguard's is in the player's play area,
     under its phase label).
   - **Prompts:** `CardList` and `SelectFromCardList`, plus `SelectCards` for hand-drag picks.
   - **Details:** the details scene (step 6).

## 8. Session scene

Copy `Games/YuGiOh/YuGiOhSession.cs` or `Games/Vanguard/Board/VanguardSession.cs`:

```csharp
var (session, services) = MySessionBuilder.Create()
    .Player1(profile, services => { Input.Activate(…); return Input; })
    .Player2(opponentProfile, services => new MyBot(…))
    .Build();
Input.SetEventBus(services.EventBus);
session.StartGame();
Input.Show(session);                       // after StartGame: the zones exist
await SessionLoop.Run(session, Input.Presenter);
```

## What fails loudly

- **At build time:**
  - a question added to the game's input provider interface and not to `IMyAnswers`/`DefaultMyAnswers`;
  - a renamed ArC type or property used in code.
- **At startup** (`BindingException`, every problem listed):
  - a zone with no view, or a view whose name matches no zone;
  - a view that can't show its kind of zone (a `CardSlot` on a group);
  - a zone bound twice.
- **While playing:**
  - a sideways card in a `CardSlot`;
  - a desync, in debug builds;
  - pre-answers nobody asked for;
  - a gesture a scope allowed that its strategy doesn't handle;
  - a missing `BoardButtonView` for a button a scope offers.

---

## Redundancy review

These were found by building Vanguard after YuGiOh. They're ordered by how much a new game would save.

### Worth merging

1. **The input provider is mostly the same code in both games.**
   - `YuGiOhInputProvider` (205 lines) and `VanguardInputProvider` (~225) share `Ask`, `StartAction`, the
     strategy swap, the inspect handlers, the list/details wiring and `Descendants`.
   - A shared base `ArcInputProvider<TAnswers, TInput>` would leave each game only its question methods and its
     phase → strategy map.
2. **Each question is written four times:** the question key, the interface method, the strategy's pass-through
   and the provider's `Ask` line.
   - The pass-through `MyStrategy` (one line per question) could be dropped if strategies held a "below" and the
     provider tried each answerer in turn.
   - That would cost a little type safety (a strategy would have to say which questions it answers), so it's a
     trade-off rather than a clear win.
3. **`YuGiOhInput` and `VanguardInput` overlap:** the board, answers, narrowing, dialog parent, pickers, details
   and `State<T>()` are the same. A shared `GameInput` base with the game adding its play areas would do.
4. **The default answers duplicate helpers:** `PickEffect`, `OwnerOf(skill)` and `EnsureAny` are copied between
   `DefaultYuGiOhAnswers` and `DefaultVanguardAnswers`. They belong in `Prompts`.
5. **The board scripts are nearly identical:** `YuGiOhBoard.Show` and `VanguardBoardView.Show` differ only in their
   hidden zones and move config. A shared `ArcBoard<TSession>` base (hands, `Sfx`, `Show`, providers) would cut
   the per-game script to a few lines.
6. **`PresentationContext` comes from two places:** in YuGiOh the input provider provides it, in Vanguard the board
   does. Pick one, the board, so a board works the same in the debug scene and the game.
7. **`Descendants(node)` is written four times:** `ArcDriver`, both input providers and `VanguardPlayAreaView`.
   It belongs in Rev-Godot-Core next to `UniqueNodes`.
8. **The "read on `Settled`, then present" pattern is repeated:** `LifePointsLabel`, `UnitStats` and
   `GuardianShields` each write the subscribe/unsubscribe/present plumbing. A small base (`SettledReader<TModel>`
   with `Read()` and `Show(value)`) would make the next one a few lines.
9. **Two line layers:** `AimIndicatorLayer` (the line while aiming) and `ZoneLinesLayer` (battle lines) both
   instantiate and place `AttackIndicator`s. One layer with "temporary" and "until cleared" lines would do both.

### Worth deciding

10. **Two hand-pick UIs (decided: keep both):** `SelectCardsComponent` (drag hand cards into a panel, Vanguard's
    mulligan) and `SelectFromCardList` (tap cards in a list, then Confirm).
11. **`TurnLabel` vs `PhaseLabel.OnlyOnItsAreasTurn`** say the same thing in two ways; YuGiOh still uses
    `TurnLabel`.
12. **Card details placement** differs (in the HUD for YuGiOh, an overlay for Vanguard). It's a layout choice, but
    `CardDetailsOverlay` could be the default so a game needs no decision.

### Done since

- **Card looks per game:** `CardGameStyle` replaced the settable `CardScenes.Create`, `SceneFactory`, the sleeve
  export on Vanguard's play area (copied to the hands in code) and the sleeve path fixed in pack opening. Vanguard's
  stat panels left the shared `Card.tscn` for `VanguardCard.tscn`.

### Removed in step 8e

The old Vanguard board and the shared classes only it used are gone. The old board's reveal holds came back as
`ArcDriver.Hold<TReason>(seconds, sides)`. `SceneFactory` stays: Vanguard's cards and `CardScenes.Create` use it.
