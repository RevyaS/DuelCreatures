using System;
using Godot;

/// <summary>
/// A soft radial glow in a rarity's color that pulses. A rarity with several colors cycles through them.
/// Placeholder art: swap the drawing for a texture or shader later, the API stays.
/// </summary>
public partial class RarityAura : Control
{
    const int Rings = 14;
    const float ColorCycleSeconds = 0.6f;
    const float PulseSpeed = 4;

    Color[] colors = [Colors.Transparent];
    double time;

    /// <summary>0 hides the glow, 1 is full strength.</summary>
    public float Intensity { get; set; } = 1;

    public float PulseAmount { get; set; } = 0.12f;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void SetRarity(Rarity? rarity) => colors = rarity?.Colors ?? [Colors.Transparent];

    public void SetColor(Color color) => colors = [color];

    public Color CurrentColor
    {
        get
        {
            if(colors.Length == 1) return colors[0];
            double cycle = time / ColorCycleSeconds;
            int index = (int)Math.Floor(cycle) % colors.Length;
            return colors[index].Lerp(colors[(index + 1) % colors.Length], (float)(cycle - Math.Floor(cycle)));
        }
    }

    public override void _Process(double delta)
    {
        time += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if(Intensity <= 0) return;

        var center = Size / 2;
        float pulse = 1 + PulseAmount * Mathf.Sin((float)time * PulseSpeed);
        float radius = Mathf.Min(Size.X, Size.Y) / 2 * pulse;
        var color = CurrentColor;

        // Big faint rings first, smaller brighter ones over them
        for(int ring = 0; ring < Rings; ring++)
        {
            float t = (float)ring / Rings;
            var ringColor = color with { A = color.A * Intensity * 0.08f * (0.3f + t) };
            DrawCircle(center, radius * (1 - t * 0.85f), ringColor);
        }
    }
}
