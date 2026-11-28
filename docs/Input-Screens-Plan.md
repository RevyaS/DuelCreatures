# Input screens: asking like Vanguard ZERO

A plan; step 1 (the shared parts, section 9) is built, the screens are not. It reworks how the client *asks* (the screens a question opens), not what it answers:
`VanguardChoices`, the strategies and `Expect` keep answering ArC's four questions exactly as now. Every screen is
built from shared scenes so YuGiOh can use the same ones.

Where the ZERO layouts come from: your description of the game, plus how ZERO is generally played. Nothing here was
checked against the game itself; the points marked **(check in ZERO)** are the ones worth a look before designing.

## 0. Rules for this work

1. Components are as reusable as possible: one scene per part, shared under `Games/Shared/HUD/Prompts/`, games only
   add layouts, their card scene and their tag/marker resources.
2. No components built in code. Every part is a `.tscn` you design; code finds nodes by `%Name` and instances the
   scenes it's given (`[Export] PackedScene`).
3. A part that shows data (cards, options) is a **base item scene instanced once per item** (`SelectableCard.tscn`
   per card, `SelectionBadge.tscn` per pick), never nodes made up in code.

And from CLAUDE.md, still: legality comes from ArC (the screens show only what's offered), strongly typed, fails
loudly, touch first.

## 1. The common shape of a ZERO screen

Every ZERO input screen is the same idea: the board goes away, the screen is split top to bottom, and the **top part is
left open for card details**. Pressing any card on the screen shows its details there; a press elsewhere closes them
and never answers the question. So:

```
[ Details space: open, card details appear here     ]
[ What is asking (the skill / the starting vanguard) ]
[ Prompt                                            ]
[ What you pick from (cards, or the field)          ]
[ Buttons: OK / Cancel                               ]
```

Shared parts that make it (all scenes):

| Part | Scene | What it is |
|---|---|---|
| Details space | `DetailsSpace.tscn` (a plain `Control`) | Marks where card details go on this screen. `CardDetailsOverlay` fits its details into the active screen's `%DetailsSpace` instead of its own fixed place; with no screen open, it uses its own place as today. |
| Asking skill | `SkillBanner.tscn` | The skill demanding the input, small: name + that skill's text only. It is a `ResolvingSkillDetails` holding a compact details scene per game (`VanguardSkillBanner.tscn`: `CardNamePart` + `SkillTextPart`). Not closable; goes when the skill finishes, as today. |
| Prompt | `PromptHeader.tscn` | The hint text and a counter ("2 / 3"). Replaces `InputHintLabel` while a screen is up. |
| Pick list | `SelectionGrid.tscn` | 1–3 rows of items (scrolls beyond), each an instance of its `[Export] PackedScene ItemScene` (`SelectableCard.tscn`). |
| One card to pick | `SelectableCard.tscn` | Holds the game's card scene (from `CardGameStyle`) at `CardScale`, and `%Overlays`: a card-sized layer (300x420, scaled with the card) holding `%Badge`, `%Tag` and `%Facing`. A tap shows the card's details and, where cards are picked, picks or drops it (picked: outlined, `PickedOutline`; can't be picked now: tinted, `DimmedTint`). API in section 7. |
| Number badge | `SelectionBadge.tscn` | The "1, 2, 3" pick number. **Extracted from `CardView.tscn`'s `BadgePanel`/`Badge`**, so the board circles and the grid show the same scene. |
| Tag | `CardTagView.tscn` | Top right of the card: where it is ("Soul", "Drop", "Hand", …) or what it's for ("Intercept"). `%Icon` and `%Text`: shows the icon, the text, or both, whichever the tag has. Section 7. |
| Facing | `FacingIndicator.tscn` | How the card will face once answered (counter blast: face down; counter charge: face up), one node per state (`%FaceDown`, `%FaceUp`). Section 7. |
| Buttons | `PromptButtons.tscn` | OK and Cancel (Cancel hidden unless the question allows declining). OK disabled until the pick count fits. Their texts are exported per screen. |
| Field focus | `FieldFocus` (script on a `Control` in the screen) | Marks the rect the field is enlarged into (screen C). |

All screens are `ICollapsiblePrompt` under the `PromptLayer`: swipe down shows the board, swipe up / tap the bar
returns, nothing is answered by collapsing (already how `PromptLayer` works; the screens just have to be in it).

One model behind every "pick, numbered, OK" (grid and board): a plain C# `CardSelection<T>` (minimum, maximum, the
ordered picks, `Toggle`, `CanConfirm`, `CanPickMore`, `NumberOf(item)`), so `ChooseZones` on the board and
`SelectionGrid` count and number the same way.

One screen at a time: no question opens a screen while another is up. A screen opening while one is showing is a bug
and fails loudly (naming the question it's still answering).

## 2. A. Swipe up to pick one: "activate?", skill order, options

**Now:** "Use this skill?" opens the effect list with one card; drag it out to use it, any press outside the list
declines. The card details sit over the list, so pressing to read them easily lands "outside" and declines by accident.

**Built** for Vanguard: `SwipeUpPicker` (shared script) with `SwipeUpOption<T>`, laid out in
`Games/Vanguard/HUD/Prompts/VanguardSwipeUpPicker.tscn`, instanced as `%SwipeUp`. A row card dragged out and let go
over `%PickArea` (a `DropArea` filling the space above the row) is the pick; `%PickHint` shows while it's over it.
"Choose one of" options show the asking skill's card once per option, the option's label as its location tag, details
focused on the option. `VanguardChoices` sends "use this skill?", skill order and options to it.

```
[ SkillBanner: the skill being used, if any; else open space for card details ]
[ Prompt                                                                      ]
[ Cards, 1 row generally (enlarged)                                           ]
[ Cancel                                                                      ]
```

A is for picking **one** card. Picking several, optional or not, is D.

- **Swipe a card up out of the row to pick it** (dragging out, as now; let go back on the row returns it). Pressing a
  card shows its details in the space above (over the `SkillBanner` when there is one).
- The top shows the skill currently being used (`SkillBanner`, as `ResolvingSkillDetails` pins it now) when there is
  one; otherwise it's open space for card details.
- A press outside **no longer declines**. Only **Cancel** declines; it's hidden when the question can't be declined.
- Cards one per option: one per skill (details focused on that skill), one per "choose one of" option.

| Question | Cards in the row | Cancel |
|---|---|---|
| "Activate this skill?" (`Confirm` with an `ActivateSkillRequest`) | the skill's card | yes: declines |
| Skill order (`Choose<ActivateSkillRequest>`) | one per waiting skill | no |
| "Choose one of" skill options | one per option ArC says can be chosen | only if the question can be cancelled |

- A `Confirm` that isn't about a skill stays a simple dialog with **Yes** / **No** (`ChoiceDialog`, as now).
- YuGiOh: the same scene for its optional triggers and chain responses' "which effect".

## 3. B. Mulligan screen

```
[ Details space (about half the height)                      ]
[ Prompt: Move the cards to exchange                          ]
[ Going 1st / 2nd ]  [ Confirm ]  [ Starting vanguard (small) ]
[ Selection field (drop area)                                 ]
[ Hand                                                        ]
```

`MulliganScreen.tscn` (Vanguard layout) from shared parts: `DetailsSpace`, `PromptHeader`, `TurnOrderLabel.tscn`,
`PromptButtons` (Confirm only), a `CardSlot`-like small holder for the starting vanguard (tap: its details), the
existing `SelectCards.tscn` as the selection field, and a `CardLineDynamic` as the hand.

- **The hand here is not the board's `HandZone`.** The driver binds one view per zone by name; a second view of the
  hand would break that. The screen gets card nodes for the hand's cards (`CardScenes.Create`), the board's hand
  `Resync()`s afterwards, as the mulligan already does.
- Gestures unchanged: drag from the hand into the field, drag back to keep, Confirm.
- The right slot is the starting vanguard now; ZERO's timer slot can be added later as another part.

Needs from ArC (check before building):
- **Who goes first, when the mulligan is asked.** If ArC decides it after the mulligan, the label can't show it.
- **The starting vanguard's card** while it's still face down: the player owns it, so showing its face to them is fine;
  the small slot shows the face whatever `IsFaceUp` says.

YuGiOh has no mulligan; the same parts would make a "go first / second" screen if one is ever asked.

## 4. C. Field selection screen (circles / units)

```
[ Details space: the asking skill's card, large (about half)   ]
[ Prompt: Choose a circle / Move to the selected circle          ]
[ Field, enlarged, offered circles highlighted, numbers on picks ]
[ Card row (only when a card goes with the circle)               ]
[ OK / Cancel (several picks or optional)                        ]
```

**Built** for Vanguard: `FieldSelectScreen` (shared script, `Games/Shared/HUD/Prompts/`), laid out in
`Games/Vanguard/HUD/Prompts/VanguardFieldSelectScreen.tscn`, instanced as `%FieldSelect` in the input provider's
`PromptLayer`. `VanguardChoices.ChooseZones` (triggers, skill targets, units picked by circle) and
`VanguardInputProvider.Assign` go to it. YuGiOh still picks zones on the board.

The field is **the real board, scaled and moved** so the play areas holding the offered spaces fill the screen's
`%FieldFocus` rect, then put back (`FieldFocus.Focus/Release`). The board's root is transformed, not the play area:
`Player1Area` sits in a `VBoxContainer`, which resets its children's scale and position whenever it lays them out. Not
a copy: the circles, their highlights, badges, drops and `ZoneClicked` stay the ones `InputBoard` already uses, and
whatever works from global positions (aim lines, card flights, drop hit tests) follows. The screen's other parts are
opaque and cover the rest of the board; `%FieldFocus` lets presses through to it.

- Which field: the play area holding the offered circles; both (rare) enlarges the whole board.
- **Orientation: the circles stay where they are on the board, the cards face you.**
  - Your field: your front row on top with your vanguard top center, your back row below (as on your board).
  - The opponent's field: their back row on top, their vanguard bottom center (as across the table on the board), but
    every card upright for you, not upside down. That's the one difference from the board: there `FacesOwner` turns
    the far side's cards to read for their owner.
  - So the area keeps its placement and `PlayAreaView.FaceViewer(bool)` turns what reads for its owner to read for the
    viewer while it's focused: half a turn more for every card holder (`CardArea`, `CardLine` `FlippedAppearance`),
    and `FacesOwner` labels as on the near side. It gives them back after. Piles (`CardVerticalStack`) aren't turned.
- The top shows the asking skill (a `ResolvingSkillDetails` with the game's full details scene, reading the same
  `ResolvingSkills` as the pinned one, which steps aside while a screen is up). A tap on a field card or a row card
  shows *its* details over that (`%DetailsSpace`), a tap elsewhere closes them (not answering). A trigger's question
  has no skill asking: triggers get their own layout with the battle on top (below, "Triggers: the battle on top").
- Swiping down collapses the layer: the board goes back to its size until the screen comes back up
  (`PromptLayer.CollapsedChanged`).
- **Card row** (`SelectionGrid` with one row): for ArC's `Assigning<TCard, TSpace>` questions (`IPlayerInput.Assign`),
  which put cards on circles in one answer, like a superior call. Which cards is asked before, as its own question
  (superior call from the deck: `Steps.PickExactly` from the deck, a D screen, then `Steps.Assign(chosen: pick, …)`).
  The row holds the question's cards, unplaced ones only, in order:
  - **Tap a highlighted circle: the leftmost card goes there** (circles offered only where that card may go,
    `question.SpacesFor(card)`). This holds however many cards the row holds, a whole deck included.
  - **Or drag any row card onto a circle** it may go to (`CanPlace`); drops are hit-tested by position, so the
    enlarged board takes them.
  - Tapping a circle holding a placed card takes it back (it returns to its place in the row). Placed circles are
    numbered in placing order. Overcalling onto an occupied circle is allowed where ArC allows it. Dragging a placed
    card back to the row isn't built.
  - Placements are previews: nothing moves in ArC until **OK** (enabled between `Minimum` and `Maximum`
    placements; Cancel when `CanCancel`); the board `Resync()`s after. The answer goes through `question.Expect`.
  - An input that doesn't override `Assign` gets ArC's `Assigning.OneByOne` (a card question, then a circle per card);
    YuGiOh's provider still does.
- Single pick: a tap answers, as now. Several: tap = numbered, tap again = drop, OK. Optional: Cancel.

Used for: skill targets, trigger power / critical / stand, interceptors outside the guard step, retire / rest
choices on units, superior calls. YuGiOh: targets on the field, tributes, Xyz materials, zone choices.

### Triggers: the battle on top

**The problem:** a trigger's question offers only the checking player's circles (`area.OccupiedCircles`), so the
screen enlarges only that play area. The other side of the battle (the unit you attack on a drive check, the unit
attacking you on a damage check) is off screen, and judging where the power or critical goes means collapsing the
layer to look. Meanwhile the top half is empty: no skill is asking.

**The layout:** `VanguardTriggerSelectScreen.tscn`, an **inherited scene** of `VanguardFieldSelectScreen.tscn` (same
`FieldSelectScreen` script), with the top half used for the battle and the trigger:

```
[ Battle: attacker card · power · critical      vs      defender's overall power ]
[ Trigger: the revealed card · what it gives ("+10000 power", "+1 critical", "Stand") ]
[ Prompt: Give the power to a unit                                               ]
[ Your field, enlarged, circles highlighted (as now)                             ]
[ OK                                                                             ]
```

- `VanguardChoices.ChooseZones` opens it for `TriggerQuestion.Power`, `.Critical` and `.Stand`; every other circle pick
  keeps the plain layout.
- **Tap previews, OK answers.** A new export on `FieldSelectScreen`, `ConfirmSinglePick` (false on the plain layout,
  true on this one): a single pick is numbered instead of answering, the battle summary and the unit's power show the
  would-be numbers, a tap on another circle moves the pick, and **OK** answers. Skill targets keep answering on one tap.
- What the preview shows:
  - Power to the attacker (drive check): the attacker's power goes up. Power to your defending unit (damage check): the
    defender's overall power goes up. Power to any other unit: the summary stays, that unit shows its would-be power on
    the field (as the guard's would-be power does now).
  - Critical to the attacker: its critical goes up.
  - Stand: no numbers change; the summary is there to see the battle.
- `%Trigger` (`VanguardTriggerCard.tscn`): the revealed trigger as a small `SelectableCard` (not selectable; a tap
  shows its details) and a `Label` with what it gives, worded from exported texts.

**Needed from ArC:** the trigger's amount. The question carries only the `TriggerQuestion` purpose; the client must not
assume +10000 / +1. Ask for the trigger card (or its amount) on the question, e.g. in its `About`. Until then the
summary shows the current numbers and the preview shows nothing.

### The battle summary (shared by triggers and guarding)

One part, `VanguardBattleSummary.tscn`, used by the trigger layout and the guard screen (section 5). Every number is a
`StatView`, so the graphics are yours to place: for now `TextureRect`s and `Label`s, the textures set in the scenes.

| Scene | Holds | Shows |
|---|---|---|
| `StatView.tscn` (shared, `Games/Shared/HUD/Prompts/`) | `%Icon` (`TextureRect`), `%Value` (`Label`) | One number with its picture. Exports: the icon texture, the value's colour, the colour of a would-be value (`PreviewColor`). |
| `VanguardAttackerView.tscn` | `%Card` (a `SelectableCard`, not selectable), `%Power` and `%Critical` (`StatView`s), plus any `TextureRect`s you add around the card | The attacking unit (a tap shows its details), its power in this battle (boost included) and its critical. |
| `VanguardDefenseView.tscn` | `%Power` (`StatView`) | The defender's **overall power: its power plus the shield on the guardian circle**. |
| `VanguardBattleSummary.tscn` | `%Attacker` (`VanguardAttackerView`), `%Defense` (`VanguardDefenseView`), anything else you place between them ("vs") | The battle. |

The API (the summary doesn't decide anything; prompts tell it what to show):

```csharp
public partial class StatView : Control
{
    /// <summary>The number shown.</summary>
    public int Value { get; set; }
    /// <summary>A would-be number shown instead, in PreviewColor; null shows Value again.</summary>
    public int? Preview { get; set; }
}

public partial class VanguardBattleSummary : Control
{
    /// <summary>The battle as it is now: attacker card, its power and critical, the defender's power plus shield.</summary>
    public void Show(VanguardBattle battle);
    /// <summary>Would-be numbers (null: as shown). Clear with ClearPreview.</summary>
    public void Preview(int? attackerPower = null, int? attackerCritical = null, int? defensePower = null);
    public void ClearPreview();
    public event Action<CardBase>? CardPressed;   // the attacker's card: details
}
```

- `VanguardBattle` is a record (attacker circle, booster circle or none, target circle) kept by a session component,
  `VanguardBattleTracker`, from the events `VanguardBattleLines` already listens to (`AttackDeclared`, `BoostDeclared`,
  `BattleEnded`). The numbers are read from ArC when shown: the attacker's power in the battle, its critical, the
  target's power and the shield on the guardian circle. **Check ArC** for one query giving the defender's power plus
  shield (and whether the attacker's power includes the boost); otherwise the summary sums them, as
  `ShowGuardedPower` does now.
- Both players' battles: on a damage check you're the defender, and the same summary reads the other way round.
- YuGiOh can reuse `StatView` for ATK against ATK/DEF later; the summary itself is Vanguard's.

## 5. D. Card selection screen

```
[ Details space (small)                ]
[ SkillBanner: just the skill asking   ]
[ Prompt                               ]
[ Cards, 1–3 rows, tag on each         ]
[ OK  (Cancel when it can be declined) ]
```

**Built** for Vanguard: `CardSelectScreen` (shared script) with `CardSelectRequest`, laid out in
`Games/Vanguard/HUD/Prompts/VanguardCardSelectScreen.tscn`, instanced as `%CardSelect` in the input provider's
`PromptLayer`. It covers the board (swipe down to look at it). `VanguardChoices.ChooseCards` sends every card question
to it except units, which go by their circles to C.

- **Tap a card: picks it** (numbered with `SelectionBadge`, outlined) **and shows its details** in the details space;
  tap it again: drops it. OK when the count fits. While a screen is up, the details overlay lets presses through, so
  the next tap reaches the next card; the details close with the screen.
- Unpicked cards can't be picked once the maximum is (`CardSelection.CanPickMore`): they're tinted, and a tap only
  shows their details.
- Optional picks (`CanCancel` / minimum 0) show **Cancel**, which answers nothing.
- The cards are enlarged (`SelectableCard`). This replaces `PickCards` and the long press on damage; skill order and
  options stay a swipe up (A).

What goes through it (Vanguard), by place, as `VanguardChoices.ChooseCards` routes now:

| Question | Now | With D |
|---|---|---|
| Soul blast, discard, cards from the drop zone | `PickCards` (drag into panel) | select + OK |
| Deck search | `DragOutOfCardList` | select + OK (Cancel when `CanCancel` / minimum 0) |
| Heal (damage zone) | `PickCards` | select + OK |
| Counter blast | long press damage on the board | select + OK; picked cards show **Face down** (`FacingIndicator`) |
| Counter charge | long press | select + OK; unpicked cards show **Face down**, picked ones **Face up** |
| Guard | drag to the guardian circle, tap interceptors | select + Guard (below) |
| Cards from several places at once | throws `UnansweredQuestion` | works: the tag tells them apart |

The last row is the point of the tag: YuGiOh's "from your hand or Deck" (or GY / banished) is one grid instead of a
question the board can't answer.

### Guard

```
[ Details space                                                          ]
[ Battle: attacker card · power · critical   vs   your overall power     ]
[ Prompt: Guard                                                          ]
[ Hand cards (Hand tag, shields shown) + front rear-guards that can intercept (Intercept tag) ]
[ Guard                         No Guard                                 ]
```

`GuardScreen.tscn` (Vanguard layout) on the shared parts plus the **battle summary** (`VanguardBattleSummary.tscn`,
section 4), the same part the trigger layout uses: the attacker with its power and critical, and your overall power
(your unit's power plus the shield). Every pick previews the overall power (`Preview(defensePower: …)`), so you see
how much more to place against the attacker's power.

- One grid for both: the hand cards that can guard (offered `CallGuardianRequest`s) with the **Hand** tag and their
  shield shown (`VanguardCardComponent.GuardMode`), and the rear-guards that can intercept (offered
  `InterceptRequest`s) with the **Intercept** tag.
- Select any number; the would-be overall power updates on every pick (the unit's power, the shield already on the
  guardian circle and the picked shields; the client's sum of the picked shields, as `ShowGuardedPower` does now).
  Shield a guardian's skill adds once placed shows after Guard.
- **Guard** answers the picks; **No Guard** passes (or Guard with nothing picked: one of the two, your call in the
  scene).
- `VanguardGuardPrompt` keeps its staging and one-request-per-question answering; only `Stage` changes, from board
  drags to this screen. No board preview any more: nothing moves until Guard, so nothing has to `Resync()`.
- The spec lines in CLAUDE.md for guarding change (section 9, step 8).

## 6. What stays on the board (not screens)

These are free actions in a phase, not questions, and already match ZERO's "drag on the field" feel:

- Ride, call, move (drag), [ACT] skills from details, attacks and boosts (aim), YuGiOh summons / sets / position
  swipes / battle.
- Tap a face-up card for details, listing zones for their card lists.
- Chain responses (YuGiOh): could later be A with one card per effect plus the `ChainView` above; not part of this
  plan.

## 7. Card indicators: the API

What a `SelectableCard` shows besides the card is set through one typed API. The looks are scenes and resources you
design; the code only says which state a card is in.

```csharp
public partial class SelectableCard : Control
{
    /// <summary>The card shown (the grid instances one SelectableCard per item and calls this).</summary>
    public void Show(CardBase card);
    public CardBase Card { get; }

    /// <summary>Picked as number N (the badge shows it, the card is outlined), or not picked (null).</summary>
    public int? PickNumber { get; set; }
    /// <summary>A tap may pick or drop the card now (false: tinted, e.g. the maximum is picked).</summary>
    public bool CanSelect { get; set; }
    /// <summary>Taps pick the card at all (false where it's only to read or to drag).</summary>
    public bool Selectable { get; set; }

    /// <summary>Where the card is or what it's for (icon and/or text), or none.</summary>
    public CardTag? Tag { get; set; }
    /// <summary>How the card will face once answered (counter blast / charge), or none.</summary>
    public FacingMark Facing { get; set; }   // None, FaceDown, FaceUp

    public event Action<SelectableCard>? SelectPressed;   // a tap while it may be picked
    public event Action<SelectableCard>? Pressed;         // a tap on the card: details
}

[GlobalClass] public partial class CardTag : Resource
{
    [Export] public string Text { get; set; } = "";
    [Export] public Texture2D? Icon { get; set; }
}

```

- **Two indicators, each a component to design:** the location (`CardTagView.tscn`: `%Panel` with `%Icon` and
  `%Text`, each shown when the tag has one; a tag with neither fails loudly) and the facing (`FacingIndicator.tscn`:
  `%FaceDown` and `%FaceUp`, each a node of its own; swap their shade and label for graphics). Both scenes' roots are
  a card at the card component's own size (300x420) and sit in `SelectableCard`'s `%Overlays`, which scales with the
  card, so they're designed as on a full-size card and keep their proportions at any `CardScale`. The pick number
  (`SelectionBadge`) is in the same layer.
- Per game, a resource with **one property per place and role**, not a dictionary of strings, so a missing one is a
  missing property, not a typo:
  - `VanguardCardTags.tres` (`VanguardCardTags : Resource`): `Hand`, `Soul`, `DropZone`, `Deck`, `DamageZone`,
    `RearGuard`, `Vanguard`, `Intercept`. `For(ICardSpace zone)` maps a zone to its tag and throws for a zone it
    doesn't know. Roles (`Intercept`) are set by the prompt that knows the role (the guard screen).
  - YuGiOh later: `YuGiOhCardTags.tres` (`Hand`, `Deck`, `Graveyard`, `Banished`, `ExtraDeck`, `Field`, …).
  - An unset property used at runtime fails loudly, naming the resource and the property.
- The tag is on `SelectableCard`, not on `VanguardCardComponent`: it's only needed while picking, and on the wrapper
  YuGiOh gets it without touching its card scene.

## 8. Every input, and where it goes

| Input | Where it goes |
|---|---|
| "Activate this skill?" | A, swipe the card up; Cancel declines |
| Any other Yes / No | a simple dialog, Yes / No (`ChoiceDialog`, as now) |
| Skill order when several trigger at once | A, one card per skill, no Cancel |
| "Choose one of" skill options | A, one card per option |
| Mulligan | B |
| Skill targets, trigger power / critical / stand | C (top shows the skill's / trigger's card) |
| Superior call, move to a circle | C with the card row |
| Soul blast, discard, drop zone, deck search, heal | D |
| Counter blast / counter charge | D with the facing indicator |
| Guard (hand guardians + interceptors) | D (`GuardScreen`), Hand / Intercept tags |
| Optional choices (`CanCancel`, minimum 0) | D with **Cancel** (answers nothing); C also gets Cancel for optional circle picks |
| `Order<T>` (put cards back in an order) | not asked by ArC yet; D's numbers can mean the order when it is |
| Opponent asking (you wait) | nothing to answer; consider a small "Opponent is choosing…" in the prompt line **(check in ZERO)** |
| Timer | the slot right of Confirm on B, later; no ArC support needed until there are time limits |
| Pressing details while a screen is up | details show in the screen's details space, close on a press elsewhere, never answer |
| Looking at the board | swipe down on any screen; swipe up / tap the bar to return |

## 9. Order of work

1. Shared parts: extract `SelectionBadge.tscn` from `CardView`, `CardSelection<T>`, `SelectableCard.tscn` with its
   API, `SelectButton.tscn`, `CardTagView.tscn`, `CardTag` / `CardMarker` and `VanguardCardTags` /
   `VanguardCardMarkers`, `SelectionGrid.tscn`, `PromptHeader`, `PromptButtons`, `DetailsSpace` and the overlay placing
   details into it.
   **Built:** all under `Games/Shared/HUD/Prompts/` (Vanguard's tags and markers under
   `Games/Vanguard/HUD/Prompts/`, placeholder marker scenes in `Markers/`). `CardView` and the board's `CardSlot` show
   their numbers with `SelectionBadge.tscn` (`ZoneInput.ShowPickNumber` instances it; `CardSlot`'s `PickNumber…` colour
   exports are gone, the look is the scene). Both games' `ChooseZones` count with `CardSelection<ICardSpace>`.
   `SelectionGrid.Show(selection, cardOf)` wires select buttons to a `CardSelection`; `PromptHeader.Follow` and
   `PromptButtons.Follow` keep the counter and OK in step with it. `VanguardCardTags`/`VanguardCardMarkers` derive from
   `CardIndicators`, whose `EnsureComplete()` names every unset property. `DetailsSpace.Showing()` is the one visible
   details space (two at once throws); `CardDetailsOverlay.Show` fits its details into it, else puts them back where
   the scene has them.
2. **A** `SwipeUpPicker` for "activate?", skill order, options (smallest, fixes the accidental decline).
   **Built** (section 2). Picking several (D) changed with it: no select buttons, a tap picks and shows the details
   (`SelectButton.tscn` is gone).
3. **D** `CardSelectScreen` for soul blast, discard, drop zone, deck search, heal; then counter blast / charge with
   the markers.
   **Built** (section 5). The markers became one facing indicator (`FacingIndicator`, `FacingMark`); `CardMarker` and
   `VanguardCardMarkers` are gone. The location tags come from `VanguardCardTags.tres`, exported on the input provider
   (`CardTags`) and checked complete at startup.
4. **Battle summary and guard:** `StatView`, `VanguardAttackerView`, `VanguardDefenseView`, `VanguardBattleSummary`,
   `VanguardBattleTracker`; then `GuardScreen` with the summary, `VanguardGuardPrompt.Stage` moved to it.
   **Built**, with the trigger layout (section 4), differently in places:
   - No tracker: the battle is read from ArC while its questions are asked, `session.Actions.Current<VanguardBattle>()`
     (attacker, target, boost in the attacker's power) and `Current<TriggerCheck>().Checked` (the revealed trigger; its
     `TriggerPowerBonus` / `TriggerCriticalBonus` are the amounts). `VanguardInput.Session` gives the session.
   - `VanguardBattleSummary` is one script over `%AttackerCard` (a `SelectableCard`), `%AttackerPower`,
     `%AttackerCritical` and `%DefensePower` (`StatView`s, shared); the attacker and defense views are groups in its
     scene, not scenes of their own.
   - `VanguardTriggerSelectScreen` subclasses `FieldSelectScreen` (its scene is a full copy of the field layout, not an
     inherited scene) with `ConfirmSinglePick` on; `FieldSelectScreen.PicksChanged` drives the preview. A power given
     to a unit outside the battle previews nothing yet (no would-be power on the field).
   - `VanguardGuardScreen` returns the picks; `VanguardGuardPrompt` still answers them one per question. The board's
     guard preview and the Guard / Return drop choices are gone.
5. **C** `FieldSelectScreen` with `FieldFocus` and `PlayAreaView.FaceViewer`; then the card row for superior calls.
   **Built** (section 4), with the screen base every screen uses: `InputScreen` (in the `PromptLayer`, one at a time,
   the error naming the question still answered; `%SwipeArea` collapses the layer) and `ResolvingSkills` (one list of
   the skills being used, read by every `ResolvingSkillDetails`).
   Next: the trigger layout, `VanguardTriggerSelectScreen.tscn` (inherited, with the battle summary and
   `%Trigger`) and `FieldSelectScreen.ConfirmSinglePick`; the would-be numbers once ArC gives the trigger's amount.
6. **B** `MulliganScreen` (after asking ArC about turn order at mulligan time).
7. YuGiOh: route its `Choose`/`Confirm` to A, C, D (`YuGiOhChoices`), with a `YuGiOhSkillBanner.tscn` and
   `YuGiOhCardTags`.
   **Built**, differently in places: the three screens' layouts moved to shared scenes (`Games/Shared/HUD/Prompts/
   SwipeUpPicker.tscn`, `CardSelectScreen.tscn`, `FieldSelectScreen.tscn`) whose skill banner is empty; each game's
   screen is an inherited scene adding its card details scene there (no separate skill banner scene). What both games
   ask the same way is shared code: `InputScreens` (finding and wiring the screens), `SkillPicks` (skill order,
   options, "activate?", yes/no), `ScreenCardSelection` (card picks with tags and facing). YuGiOh's chain responses are
   A too: `YuGiOhSwipeUpPicker` adds the chain above the prompt (`%ChainSection`, `%Chain`). Its details moved into a
   `CardDetailsOverlay` (`YuGiOhCardDetailsOverlay.tscn`, `ClosesOnPressOutside` off: the board leaves them a space of
   their own), and the field selection screen enlarges `%BoardLayout`, the board's container, which no container holds.
   Tributes and Xyz materials picked during a summon the player started stay on the board.
8. Update the input scenarios in `CLAUDE.md`, `docs/ArC-Rewrite-Adoption-Guide.md` 4.3 / 4.4 / 4.5 / 12.4–12.5:
   picking several cards becomes "select button, numbered, OK"; swiping up out of the row stays for one-of picks,
   with Cancel; guarding and counter blast / charge become D.

Each step leaves the game playable: the new screen takes over the questions it covers, the rest stay on the old
prompts until their step.

## 10. Open questions

None left from the design pass. To check while building: the ArC points above (turn order at mulligan time, the
trigger's amount on its question, one query for the defender's power plus shield) and the **(check in ZERO)** notes.
