# Input screens: asking like Vanguard ZERO

A plan, nothing built yet. It reworks how the client *asks* (the screens a question opens), not what it answers:
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
| One card to pick | `SelectableCard.tscn` | Holds the game's card scene (from `CardGameStyle`), **enlarged** (its size set in the scene), and slots: `%SelectButton`, `%Badge`, `%Tag`, `%Marker`. A tap on the card shows its details; the select button picks it. API in section 7. |
| Select button | `SelectButton.tscn` | The small button that picks / unpicks its card. The scene's root is any `BaseButton` in toggle mode, so it can be a `TextureButton` with your images (unpicked, picked, disabled) or a plain `Button`. Code only sets `ButtonPressed` and `Disabled`. |
| Number badge | `SelectionBadge.tscn` | The "1, 2, 3" pick number. **Extracted from `CardView.tscn`'s `BadgePanel`/`Badge`**, so the board circles and the grid show the same scene. |
| Tag | `CardTagView.tscn` | Top right of the card: where it is ("Soul", "Drop", "Hand", …) or what it's for ("Intercept"). `%Icon` and `%Text`: shows the icon, the text, or both, whichever the tag has. Section 7. |
| Marker | (a scene per marker) | A state drawn over the card, e.g. counter blast's "will turn face down". The marker resource names its scene; it's instanced into `%Marker`. Section 7. |
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

**Plan:** `SwipeUpPicker.tscn` (the current drag-out gesture, in the ZERO layout, with a real Cancel)

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

`FieldSelectScreen.tscn`. The field is **the real play area view, moved and scaled** into the screen's `%FieldFocus`
rect while the screen is up, then put back. Not a copy: the circles, their highlights, badges and `ZoneClicked` stay
the ones `InputBoard` already uses, so `ChooseZones` keeps working unchanged under the new look.

- Which field: the play area holding the offered circles; both (rare) enlarges the whole board.
- **Orientation: the circles stay where they are on the board, the cards face you.**
  - Your field: your front row on top with your vanguard top center, your back row below (as on your board).
  - The opponent's field: their back row on top, their vanguard bottom center (as across the table on the board), but
    every card upright for you, not upside down. That's the one difference from the board: there `FacesOwner` turns
    the far side's cards to read for their owner.
  - So `FieldFocus` keeps the area's placement and asks the play area view to turn its cards to face the viewer while
    it's focused (`PlayAreaView.FaceViewer(bool)`: overrides `QuarterTurnContainer.FacesOwner` for the area's cards),
    and gives it back after.
- The top shows the asking skill's card large (the `SkillBanner`'s full version). A tap on a field card or a row card
  shows *its* details over that, a tap elsewhere closes them (not answering).
- **Card row** (`SelectionGrid` with one row): for answers that put a card on a circle, like a superior call or
  "move a card to a circle". Pick a card (select button), then tap a circle. **(check ArC)** whether those are one
  question (card + circle) or two (`Choose<CardBase>`, then `Choose<circle>`); with two, the row shows the card picked
  in the first while the second is asked.
- Single pick: a tap answers, as now. Several: tap = numbered, tap again = drop, OK. Optional: Cancel.

Used for: skill targets, trigger power / critical / stand, interceptors outside the guard step, retire / rest
choices on units, superior calls. YuGiOh: targets on the field, tributes, Xyz materials, zone choices.

## 5. D. Card selection screen

```
[ Details space (small)                ]
[ SkillBanner: just the skill asking   ]
[ Prompt                               ]
[ Cards, 1–3 rows, tag on each         ]
[ OK  (Cancel when it can be declined) ]
```

`CardSelectScreen.tscn`.

- **Tap a card: its details** in the details space. **Its select button: picks it** (numbered with
  `SelectionBadge`); the button again: drops it. OK when the count fits.
- Select buttons are disabled on unpicked cards once the maximum is picked (`CardSelection.CanPickMore`).
- Optional picks (`CanCancel` / minimum 0) show **Cancel**, which answers nothing.
- The cards are enlarged (`SelectableCard`). This replaces `PickCards` and the long press on damage; skill order and
  options stay a swipe up (A).

What goes through it (Vanguard), by place, as `VanguardChoices.ChooseCards` routes now:

| Question | Now | With D |
|---|---|---|
| Soul blast, discard, cards from the drop zone | `PickCards` (drag into panel) | select + OK |
| Deck search | `DragOutOfCardList` | select + OK (Cancel when `CanCancel` / minimum 0) |
| Heal (damage zone) | `PickCards` | select + OK |
| Counter blast | long press damage on the board | select + OK; picked cards get the "will turn face down" marker |
| Counter charge | long press | same, marker "will turn face up" |
| Guard | drag to the guardian circle, tap interceptors | select + Guard (below) |
| Cards from several places at once | throws `UnansweredQuestion` | works: the tag tells them apart |

The last row is the point of the tag: YuGiOh's "from your hand or Deck" (or GY / banished) is one grid instead of a
question the board can't answer.

### Guard

```
[ Details space                                         ]
[ Attacker power  →  Guarded unit's would-be power       ]
[ Prompt: Guard                                         ]
[ Hand cards (Hand tag, shields shown) + front rear-guards that can intercept (Intercept tag) ]
[ Guard                         No Guard                ]
```

`GuardScreen.tscn` (Vanguard layout) on the shared parts plus `GuardTotal.tscn` (the power line).

- One grid for both: the hand cards that can guard (offered `CallGuardianRequest`s) with the **Hand** tag and their
  shield shown (`VanguardCardComponent.GuardMode`), and the rear-guards that can intercept (offered
  `InterceptRequest`s) with the **Intercept** tag.
- Select any number; the would-be power updates on every pick (the client's sum of the picked shields, as
  `ShowGuardedPower` does now).
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

    /// <summary>Picked as number N (the badge shows it), or not picked (null: badge hidden, select button up).</summary>
    public int? PickNumber { get; set; }
    /// <summary>The select button can be pressed (false: shown disabled, e.g. the maximum is picked).</summary>
    public bool CanSelect { get; set; }
    /// <summary>Shows the select button at all (false on screens where the card is only to read).</summary>
    public bool Selectable { get; set; }

    /// <summary>Where the card is or what it's for (icon and/or text), or none.</summary>
    public CardTag? Tag { get; set; }
    /// <summary>A state drawn over the card (its scene is instanced into %Marker), or none.</summary>
    public CardMarker? Marker { get; set; }

    public event Action<SelectableCard>? SelectPressed;   // the select button
    public event Action<SelectableCard>? Pressed;         // a tap on the card: details
}

[GlobalClass] public partial class CardTag : Resource
{
    [Export] public string Text { get; set; } = "";
    [Export] public Texture2D? Icon { get; set; }
}

[GlobalClass] public partial class CardMarker : Resource
{
    [Export] public PackedScene Scene { get; set; } = null!;
}
```

- `CardTagView.tscn` shows the tag's icon, text, or both (both may be set; an empty one is hidden). A tag with neither
  fails loudly.
- Per game, a resource with **one property per place and role**, not a dictionary of strings, so a missing one is a
  missing property, not a typo:
  - `VanguardCardTags.tres` (`VanguardCardTags : Resource`): `Hand`, `Soul`, `DropZone`, `Deck`, `DamageZone`,
    `RearGuard`, `Vanguard`, `Intercept`. `For(ICardSpace zone)` maps a zone to its tag and throws for a zone it
    doesn't know. Roles (`Intercept`) are set by the prompt that knows the role (the guard screen).
  - `VanguardCardMarkers.tres`: `WillTurnFaceDown` (counter blast), `WillTurnFaceUp` (counter charge).
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
| Counter blast / counter charge | D with markers |
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
2. **A** `SwipeUpPicker` for "activate?", skill order, options (smallest, fixes the accidental decline).
3. **D** `CardSelectScreen` for soul blast, discard, drop zone, deck search, heal; then counter blast / charge with
   the markers.
4. **Guard** on D: `GuardScreen` + `GuardTotal`, `VanguardGuardPrompt.Stage` moved to it.
5. **C** `FieldSelectScreen` with `FieldFocus` and `PlayAreaView.FaceViewer`; then the card row for superior calls.
6. **B** `MulliganScreen` (after asking ArC about turn order at mulligan time).
7. YuGiOh: route its `Choose`/`Confirm` to A, C, D (`YuGiOhChoices`), with a `YuGiOhSkillBanner.tscn` and
   `YuGiOhCardTags`.
8. Update the input scenarios in `CLAUDE.md`, `docs/ArC-Rewrite-Adoption-Guide.md` 4.3 / 4.4 / 4.5 / 12.4–12.5:
   picking several cards becomes "select button, numbered, OK"; swiping up out of the row stays for one-of picks,
   with Cancel; guarding and counter blast / charge become D.

Each step leaves the game playable: the new screen takes over the questions it covers, the rest stay on the old
prompts until their step.

## 10. Open questions

None left from the design pass. To check while building: the ArC points above (turn order at mulligan time, superior
call as one question or two) and the **(check in ZERO)** notes.
