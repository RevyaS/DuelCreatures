using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DuelCreatures.Data;
using Godot;

/// <summary>
/// Opens a pulled pack: the aura climbs the tiers up to the best rarity the pack shows, the pack bursts, the cards are
/// dealt face down in an arc, late hits jolt into their real rarity, then each card is flipped (big flips for the hits).
/// Every step raises a <see cref="GachaCue"/>: <see cref="CuePlayed"/>, its stream in <see cref="GachaConfig.SfxStreams"/>,
/// and the EffectsPlayer animation named by <see cref="GachaConfig.EffectName"/> when it exists.
/// </summary>
public partial class PackOpening : Control
{
    const int SfxVoices = 4;
    static readonly Vector2 PackSize = new(420, 700);
    static readonly Vector2 AnticipationAuraSize = new(1100, 1100);

    Control Stage = null!;
    ColorRect Flash = null!;
    Label TapHint = null!, ResultsSummary = null!;
    Button RevealAllButton = null!, ContinueButton = null!;
    Control Results = null!;
    VBoxContainer ResultsList = null!;
    AnimationPlayer EffectsPlayer = null!;

    readonly List<AudioStreamPlayer> voices = [];
    readonly Dictionary<string, AudioStream?> streams = [];
    readonly List<GachaCardSlot> slots = [];
    SleeveInfo sleeve = null!;
    PackResult? result;
    TaskCompletionSource? tap;
    bool busy;

    public event Action? Finished;
    /// <summary>Every step of the opening, for effects set up in code.</summary>
    public event Action<GachaCue, Rarity?>? CuePlayed;

    public override void _Ready()
    {
        Stage = GetNode<Control>($"%{nameof(Stage)}");
        Flash = GetNode<ColorRect>($"%{nameof(Flash)}");
        TapHint = GetNode<Label>($"%{nameof(TapHint)}");
        ResultsSummary = GetNode<Label>($"%{nameof(ResultsSummary)}");
        RevealAllButton = GetNode<Button>($"%{nameof(RevealAllButton)}");
        ContinueButton = GetNode<Button>($"%{nameof(ContinueButton)}");
        Results = GetNode<Control>($"%{nameof(Results)}");
        ResultsList = GetNode<VBoxContainer>($"%{nameof(ResultsList)}");
        EffectsPlayer = GetNode<AnimationPlayer>($"%{nameof(EffectsPlayer)}");
        sleeve = GD.Load<SleeveInfo>("res://Sleeves/VanguardDefaultSleeve.tres");

        // A tap anywhere on the opening moves it along
        var gestures = new GestureDetector();
        gestures.Tapped += () => tap?.TrySetResult();
        AddChild(gestures);

        RevealAllButton.Pressed += RevealAll;
        ContinueButton.Pressed += Close;

        for(int i = 0; i < SfxVoices; i++)
        {
            var voice = new AudioStreamPlayer();
            AddChild(voice);
            voices.Add(voice);
        }
    }

    public async void Open(PackResult packResult, Texture2D? packTexture)
    {
        result = packResult;
        Reset();
        Show();

        // No reveals until every card is dealt and every late hit has played
        busy = true;
        await Anticipate(packResult.HintedRarity, packTexture);
        await DealArc(packResult.Cards);
        await PlayLateHits();
        busy = false;

        RevealAllButton.Show();
    }

    private void Reset()
    {
        Stage.FreeChildren();
        slots.Clear();
        ResultsList.FreeChildren();
        Results.Hide();
        RevealAllButton.Hide();
        TapHint.Hide();
        Flash.Color = Flash.Color with { A = 0 };
        busy = false;
    }

    #region Anticipation
    private async Task Anticipate(Rarity hinted, Texture2D? packTexture)
    {
        var center = Stage.Size / 2;
        var aura = new RarityAura { Size = AnticipationAuraSize, Position = center - AnticipationAuraSize / 2, PivotOffset = AnticipationAuraSize / 2, Intensity = 0 };
        Stage.AddChild(aura);
        var pack = CreatePack(packTexture);
        pack.Position = center - PackSize / 2;
        Stage.AddChild(pack);

        int topTier = GachaConfig.Rarities[^1].Tier;
        foreach(var rarity in GachaConfig.Rarities.Where(rarity => rarity.Tier <= hinted.Tier))
        {
            PlayCue(GachaCue.AnticipationStep, rarity);
            aura.SetRarity(rarity);

            float strength = (rarity.Tier + 1f) / (topTier + 1);
            var timing = GachaConfig.AnticipationStep;
            var tween = CreateTween().SetParallel();
            tween.TweenMethod(Callable.From<float>(value => aura.Intensity = value), aura.Intensity, 0.3f + 0.7f * strength, timing.Duration)
                .SetTrans(timing.Ease.ToTransitionType()).SetEase(timing.Ease.ToEaseType());
            tween.TweenProperty(aura, "scale", Vector2.One * (1 + 0.25f * strength), timing.Duration)
                .SetTrans(timing.Ease.ToTransitionType()).SetEase(timing.Ease.ToEaseType());
            await Task.WhenAll(Await(tween), Shake(pack, GachaConfig.AnticipationShake * (rarity.Tier + 1), timing.Duration));
        }

        TapHint.Text = "Tap to open";
        TapHint.Show();
        await WaitForTap();
        TapHint.Hide();

        PlayCue(GachaCue.PackBurst, hinted);
        var burst = GachaConfig.PackBurst;
        var burstTween = CreateTween().SetParallel();
        burstTween.TweenProperty(pack, "scale", Vector2.One * 1.4f, burst.Duration).SetTrans(burst.Ease.ToTransitionType()).SetEase(burst.Ease.ToEaseType());
        burstTween.TweenProperty(pack, "modulate:a", 0f, burst.Duration);
        burstTween.TweenMethod(Callable.From<float>(value => aura.Intensity = value), aura.Intensity, 0f, burst.Duration);
        await Task.WhenAll(Await(burstTween), FlashScreen(Colors.White, burst.Duration * 2));

        pack.QueueFree();
        aura.QueueFree();
    }

    /// <summary>The pack's art, or a plain stand-in while the set has none.</summary>
    private static Control CreatePack(Texture2D? texture)
    {
        Control pack = texture is null
            ? new ColorRect { Color = new Color(0.3f, 0.3f, 0.4f) }
            : new TextureRect { Texture = texture, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered };
        pack.Size = PackSize;
        pack.PivotOffset = PackSize / 2;
        pack.MouseFilter = MouseFilterEnum.Ignore;
        return pack;
    }
    #endregion

    #region Arc
    private async Task DealArc(IReadOnlyList<PackCard> cards)
    {
        var catalog = DataManager.Get(this).Cards;
        int count = cards.Count;
        // Fan width = one card plus a step per other card, shrunk to fit when needed
        float stepFactor = 1 - GachaConfig.ArcOverlap;
        float fanWidth = SizeConstants.CardDefaultSize.X * (1 + stepFactor * (count - 1));
        float scale = Mathf.Min(GachaConfig.ArcCardScale, GachaConfig.ArcMaxWidth / fanWidth);
        // The step is an arc length, so the cards keep their spacing however round the arc is
        float angleStep = SizeConstants.CardDefaultSize.X * scale * stepFactor / GachaConfig.ArcRadius;
        var dealFrom = Stage.Size / 2;
        var arcCenter = dealFrom + new Vector2(0, GachaConfig.ArcOffsetY + GachaConfig.ArcRadius);

        var timing = GachaConfig.ArcDeal;
        var tween = CreateTween().SetParallel();
        for(int i = 0; i < count; i++)
        {
            var view = CardView.Create(cards[i].CardId, catalog.Create(cards[i].CardId), scale);
            var slot = GachaCardSlot.Create(cards[i], view, sleeve);
            // A held press reveals too, not only a tap
            view.Pressed += _ => OnSlotPressed(slot);
            view.LongPressed += _ => OnSlotPressed(slot);

            float angle = (i - (count - 1) / 2f) * angleStep;
            var restCenter = arcCenter + new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle)) * GachaConfig.ArcRadius;
            slot.RestPosition = restCenter - slot.Size / 2;
            slot.RestRotation = angle;

            slot.Position = dealFrom - slot.Size / 2;
            slot.Scale = Vector2.Zero;
            Stage.AddChild(slot);
            slots.Add(slot);

            float delay = timing.Delay + timing.Stagger * i;
            tween.TweenCallback(Callable.From(() => PlayCue(GachaCue.CardDealt, null))).SetDelay(delay);
            tween.TweenProperty(slot, "position", slot.RestPosition, timing.Duration).SetDelay(delay).SetTrans(timing.Ease.ToTransitionType()).SetEase(timing.Ease.ToEaseType());
            tween.TweenProperty(slot, "rotation", angle, timing.Duration).SetDelay(delay).SetTrans(timing.Ease.ToTransitionType()).SetEase(timing.Ease.ToEaseType());
            tween.TweenProperty(slot, "scale", Vector2.One, timing.Duration).SetDelay(delay).SetTrans(timing.Ease.ToTransitionType()).SetEase(timing.Ease.ToEaseType());
        }
        await Await(tween);
    }

    private async Task PlayLateHits()
    {
        foreach(var slot in slots.Where(slot => slot.PackCard.LateHitFrom is not null))
        {
            var timing = GachaConfig.LateHit;
            await ToSignal(GetTree().CreateTimer(timing.Delay), SceneTreeTimer.SignalName.Timeout);

            var rarity = slot.PackCard.Rarity;
            PlayCue(GachaCue.LateHit, rarity);
            slot.MoveToFront();

            // The card jolts, then lights up in the rarity it really is
            var jolt = CreateTween();
            for(int i = 0; i < 6; i++)
            {
                jolt.TweenProperty(slot, "rotation", slot.RestRotation + (i % 2 == 0 ? 0.12f : -0.12f), timing.Duration / 12);
            }
            jolt.TweenProperty(slot, "rotation", slot.RestRotation, timing.Duration / 12);
            jolt.TweenCallback(Callable.From(() => slot.ShowRarity(rarity)));
            jolt.TweenProperty(slot, "scale", Vector2.One * 1.15f, timing.Duration / 6).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            jolt.TweenProperty(slot, "scale", Vector2.One, timing.Duration / 6);
            await Task.WhenAll(Await(jolt), FlashScreen(rarity.Color, timing.Duration / 2));
        }
    }
    #endregion

    #region Reveal
    private async void OnSlotPressed(GachaCardSlot slot)
    {
        if(busy || slot.Revealed || result is null) return;
        busy = true;
        await Reveal(slot);
        busy = false;
        ShowResultsWhenDone();
    }

    private async void RevealAll()
    {
        if(busy) return;
        busy = true;
        RevealAllButton.Hide();
        foreach(var slot in slots.Where(slot => !slot.Revealed).ToList())
        {
            await Reveal(slot);
        }
        busy = false;
        ShowResultsWhenDone();
    }

    private Task Reveal(GachaCardSlot slot)
    {
        slot.Revealed = true;
        return slot.PackCard.Rarity.Tier >= GachaConfig.BigRevealFrom.Tier ? BigFlip(slot) : Flip(slot, GachaConfig.Flip);
    }

    private async Task Flip(GachaCardSlot slot, Transition timing)
    {
        var rarity = slot.PackCard.Rarity;
        var restScale = slot.Scale;
        var tween = CreateTween();
        tween.TweenProperty(slot, "scale:x", 0f, timing.Duration / 2).SetTrans(timing.Ease.ToTransitionType()).SetEase(Tween.EaseType.In);
        tween.TweenCallback(Callable.From(() =>
        {
            slot.View.IsFront = true;
            PlayCue(GachaCue.Flip, rarity);
        }));
        tween.TweenProperty(slot, "scale:x", restScale.X, timing.Duration / 2).SetTrans(timing.Ease.ToTransitionType()).SetEase(Tween.EaseType.Out);
        await Await(tween);
    }

    /// <summary>The card comes to the middle, grows, flips slowly in a flash of its rarity, holds, then goes back.</summary>
    private async Task BigFlip(GachaCardSlot slot)
    {
        var rarity = slot.PackCard.Rarity;
        var timing = GachaConfig.BigReveal;
        slot.MoveToFront();

        var toCenter = CreateTween().SetParallel();
        toCenter.TweenProperty(slot, "position", Stage.Size / 2 - slot.Size / 2, timing.Duration).SetTrans(timing.Ease.ToTransitionType()).SetEase(timing.Ease.ToEaseType());
        toCenter.TweenProperty(slot, "rotation", 0f, timing.Duration);
        toCenter.TweenProperty(slot, "scale", Vector2.One * GachaConfig.BigRevealScale, timing.Duration).SetTrans(timing.Ease.ToTransitionType()).SetEase(timing.Ease.ToEaseType());
        await Await(toCenter);

        PlayCue(GachaCue.BigReveal, rarity);
        await Task.WhenAll(Flip(slot, timing with { Duration = timing.Duration * 1.5f }), FlashScreen(rarity.Color, timing.Duration));
        await ToSignal(GetTree().CreateTimer(GachaConfig.BigRevealHold), SceneTreeTimer.SignalName.Timeout);

        var back = CreateTween().SetParallel();
        back.TweenProperty(slot, "position", slot.RestPosition, timing.Duration).SetTrans(timing.Ease.ToTransitionType()).SetEase(timing.Ease.ToEaseType());
        back.TweenProperty(slot, "rotation", slot.RestRotation, timing.Duration);
        back.TweenProperty(slot, "scale", Vector2.One, timing.Duration).SetTrans(timing.Ease.ToTransitionType()).SetEase(timing.Ease.ToEaseType());
        await Await(back);
    }
    #endregion

    #region Results
    private void ShowResultsWhenDone()
    {
        if(result is null || slots.Any(slot => !slot.Revealed)) return;
        RevealAllButton.Hide();

        var catalog = DataManager.Get(this).Cards;
        foreach(var card in result.Cards.OrderByDescending(card => card.Rarity.Tier))
        {
            var notes = new List<string>();
            if(card.IsNew) notes.Add("NEW");
            if(card.LateHitFrom is not null) notes.Add("Late hit!");
            if(card.DustedCopies > 0) notes.Add($"+{card.DustTickets} tickets (dusted)");

            var row = new Label { Text = $"[{card.Rarity.Id}] {catalog.GetData(card.CardId).Name}{(notes.Count > 0 ? "  " + string.Join(", ", notes) : string.Empty)}" };
            row.AddThemeFontSizeOverride("font_size", 32);
            row.AddThemeColorOverride("font_color", card.Rarity.Color);
            ResultsList.AddChild(row);
        }
        ResultsSummary.Text = $"Spent {result.TicketsSpent} tickets" + (result.DustTickets > 0 ? $", got {result.DustTickets} back from dust" : string.Empty);
        Results.Show();
    }

    private void Close()
    {
        Reset();
        Hide();
        result = null;
        Finished?.Invoke();
    }
    #endregion

    #region Helpers
    private void PlayCue(GachaCue cue, Rarity? rarity)
    {
        CuePlayed?.Invoke(cue, rarity);

        if(GetStream(cue, rarity) is AudioStream stream)
        {
            var voice = voices.Find(voice => !voice.Playing) ?? voices[0];
            voice.Stream = stream;
            voice.Play();
        }

        var effect = GachaConfig.EffectName(cue, rarity);
        if(EffectsPlayer.HasAnimation(effect)) EffectsPlayer.Play(effect);
        else if(EffectsPlayer.HasAnimation(GachaConfig.EffectName(cue, null))) EffectsPlayer.Play(GachaConfig.EffectName(cue, null));
    }

    private AudioStream? GetStream(GachaCue cue, Rarity? rarity)
    {
        string key = GachaConfig.EffectName(cue, rarity);
        if(streams.TryGetValue(key, out var cached)) return cached;

        string? path = (rarity is not null && GachaConfig.SfxStreams.TryGetValue((cue, rarity.Id), out var forRarity)) ? forRarity
            : GachaConfig.SfxStreams.GetValueOrDefault((cue, null));
        var stream = path is not null && ResourceLoader.Exists(path) ? ResourceLoader.Load<AudioStream>(path) : null;
        streams[key] = stream;
        return stream;
    }

    private async Task Shake(Control target, float amplitude, float duration)
    {
        var rest = target.Position;
        var tween = CreateTween();
        const int Steps = 10;
        for(int i = 0; i < Steps; i++)
        {
            var offset = new Vector2((float)GD.RandRange(-amplitude, amplitude), (float)GD.RandRange(-amplitude, amplitude));
            tween.TweenProperty(target, "position", rest + offset, duration / (Steps + 1));
        }
        tween.TweenProperty(target, "position", rest, duration / (Steps + 1));
        await Await(tween);
    }

    private async Task FlashScreen(Color color, float duration)
    {
        Flash.Color = color with { A = 0 };
        var tween = CreateTween();
        tween.TweenProperty(Flash, "color:a", 0.8f, duration * 0.3f);
        tween.TweenProperty(Flash, "color:a", 0f, duration * 0.7f);
        await Await(tween);
    }

    private Task WaitForTap()
    {
        tap = new TaskCompletionSource();
        return tap.Task;
    }

    private async Task Await(Tween tween) => await ToSignal(tween, Tween.SignalName.Finished);
    #endregion
}
