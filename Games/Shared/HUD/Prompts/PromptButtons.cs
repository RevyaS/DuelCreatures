using System;
using Godot;

/// <summary>
/// An input screen's OK (%Ok) and Cancel (%Cancel). Cancel shows only while the question may be declined
/// (<see cref="CanCancel"/>); OK is disabled until the answer fits (<see cref="CanConfirm"/>, or a followed
/// CardSelection's count). Their texts are exported, set per screen. Laid out in PromptButtons.tscn.
/// </summary>
public partial class PromptButtons : Control
{
    [Export] public string OkText { get; set; } = "OK";
    [Export] public string CancelText { get; set; } = "Cancel";

    Button? ok, cancel;
    Button Ok => ok ??= GetNode<Button>("%Ok");
    Button Cancel => cancel ??= GetNode<Button>("%Cancel");
    Action? unfollow;

    public event Action? Confirmed;
    public event Action? Cancelled;

    public override void _Ready()
    {
        Ok.Text = OkText;
        Cancel.Text = CancelText;
        Ok.Pressed += () => Confirmed?.Invoke();
        Cancel.Pressed += () => Cancelled?.Invoke();
    }

    /// <summary>OK can be pressed.</summary>
    public bool CanConfirm
    {
        get => !Ok.Disabled;
        set => Ok.Disabled = !value;
    }

    /// <summary>Cancel shows (the question may be declined).</summary>
    public bool CanCancel
    {
        get => Cancel.Visible;
        set => Cancel.Visible = value;
    }

    /// <summary>Whether OK shows at all (e.g. a screen whose only answer is Cancel or a gesture).</summary>
    public bool ShowsOk
    {
        get => Ok.Visible;
        set => Ok.Visible = value;
    }

    /// <summary>OK is enabled while <paramref name="selection"/> can be confirmed, until <see cref="Unfollow"/> or another Follow.</summary>
    public void Follow<T>(CardSelection<T> selection) where T : class
    {
        Unfollow();
        void Update() => CanConfirm = selection.CanConfirm;
        selection.Changed += Update;
        unfollow = () => selection.Changed -= Update;
        Update();
    }

    public void Unfollow()
    {
        unfollow?.Invoke();
        unfollow = null;
    }

    public override void _ExitTree() => Unfollow();
}
