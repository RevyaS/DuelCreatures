using System;
using Godot;

/// <summary>
/// A screen a question opens over the board (field selection, card selection, …), in the PromptLayer: swiping down on
/// its %SwipeArea shows the board, swiping up or tapping the bar returns, and collapsing never answers. Only one screen
/// is up at a time: opening one while another is up is a bug and fails, naming the question the other is answering.
/// Its subclass opens it with <see cref="UpdatePrompt"/> when a question comes and closes it with <see cref="Close"/>.
/// </summary>
public abstract partial class InputScreen : Control, ICollapsiblePrompt
{
    static InputScreen? current;
    static string currentQuestion = "";

    /// <summary>The screen up now, or null.</summary>
    public static InputScreen? Current => current;
    /// <summary>A screen opened or closed.</summary>
    public static event Action? CurrentChanged;

    PromptLayer? layer;
    Control? swipeArea;

    /// <summary>Where a swipe down collapses the layer: the screen's %SwipeArea (a part that takes presses, e.g. its top panel).</summary>
    public Control SwipeArea => swipeArea ??= GetNode<Control>("%SwipeArea");
    public bool IsShowing => Visible;

    /// <summary>Whether the PromptLayer is collapsed now (the board is being looked at).</summary>
    protected bool IsCollapsed => layer?.IsCollapsed == true;

    /// <summary>The PromptLayer holding the screen, if any.</summary>
    protected PromptLayer? Layer => layer;

    public override void _Ready()
    {
        Hide();
        for(var node = GetParent(); node is not null; node = node.GetParent())
        {
            if(node is not PromptLayer found) continue;
            layer = found;
            layer.CollapsedChanged += OnCollapsedChanged;
            break;
        }
    }

    public override void _ExitTree()
    {
        if(layer is not null) layer.CollapsedChanged -= OnCollapsedChanged;
        if(ReferenceEquals(current, this)) SetCurrentPrompt(null, "");
    }

    /// <summary>Shows the screen for <paramref name="question"/> (named in the error if another screen opens meanwhile).</summary>
    protected void UpdatePrompt(string question)
    {
        if(current is not null)
        {
            throw new InvalidOperationException($"{GetPath()} can't open for \"{question}\": {current.GetPath()} is still up, answering \"{currentQuestion}\"");
        }
        SetCurrentPrompt(this, question);
        Show();
        MoveToFront();
    }

    protected void Close()
    {
        if(!ReferenceEquals(current, this)) return;
        Hide();
        SetCurrentPrompt(null, "");
    }

    /// <summary>The layer collapsed (the board shows) or expanded again, while this screen is up.</summary>
    protected virtual void OnCollapsedChanged(bool collapsed) {}

    private static void SetCurrentPrompt(InputScreen? screen, string question)
    {
        current = screen;
        currentQuestion = question;
        CurrentChanged?.Invoke();
    }
}
