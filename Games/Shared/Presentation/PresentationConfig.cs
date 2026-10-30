using System.Collections.Generic;

// Presentation settings every game shares: who gets animations/Sfx, and the shared timings.
// Reads like CSS: Transition ~ `transition: <duration> <timing-function> <delay>`,
// Keyframes ~ `@keyframes`. A game adds its own moves and sounds with Register
// (e.g. VanguardPresentationConfig).
public static class PresentationConfig
{
    // Who gets what
    public static BoardSide Animations = BoardSide.Both;
    public static BoardSide Sfx = BoardSide.Both;

    // transition: ...
    public static readonly Dictionary<CardMove, Transition> Moves = new()
    {
        [CardMoves.DeckToHand] = new(0.35f, Easing.EaseOut, Stagger: 0.08f),
        [CardMoves.ToHand]     = new(0.35f, Easing.EaseInOut),
        [CardMoves.ToDeck]     = new(0.30f, Easing.EaseIn, Stagger: 0.06f),
    };

    // For a move no game gave a timing
    public static Transition DefaultMove = new(0.30f, Easing.EaseInOut);

    public static Transition TimingOf(CardMove move) => Moves.TryGetValue(move, out var timing) ? timing : DefaultMove;

    // @keyframes flip (the face swaps at the frame where ScaleX is 0)
    public static readonly Keyframes Flip = new(new(0.25f, Easing.EaseInOut),
        new(0.0f),
        new(0.5f, ScaleX: 0f),
        new(1.0f));

    // @keyframes shuffle
    public static readonly Keyframes Shuffle = new(new(0.5f, Easing.EaseInOut, Stagger: 0.06f),
        new(0.0f),
        new(0.33f, Offset: new(-30, -20), RotationDeg: -8),
        new(0.66f, Offset: new(30, -20), RotationDeg: 8),
        new(1.0f));

    // A stat changing on a card (e.g. power): the number counts to the new value...
    public static readonly Transition StatChange = new(0.30f, Easing.EaseOut);

    // ...while its panel pulses (@keyframes pulse)
    public static readonly Keyframes StatChangePulse = new(new(0.30f, Easing.EaseOut),
        new(0.0f),
        new(0.4f, Scale: 1.25f),
        new(1.0f));

    // Sfx cue -> stream; games add theirs (cues without a stream are silent)
    public static readonly Dictionary<SfxCue, string> SfxStreams = new();

    // Simultaneous sounds before the oldest one gets cut off
    public static int SfxVoices = 4;

    // Adds (or replaces) a game's moves and sounds
    public static void Register(IReadOnlyDictionary<CardMove, Transition> moves, IReadOnlyDictionary<SfxCue, string> sfxStreams)
    {
        foreach(var (move, timing) in moves)
        {
            Moves[move] = timing;
        }
        foreach(var (cue, stream) in sfxStreams)
        {
            SfxStreams[cue] = stream;
        }
    }
}
