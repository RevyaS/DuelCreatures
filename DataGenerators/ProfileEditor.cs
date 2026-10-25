using Godot;

/// <summary>Dev scene, run it from the editor to change a saved profile's tickets, or throw away its gacha boxes.</summary>
public partial class ProfileEditor : Control
{
    DataManager DataManager = null!;

    Label ProfileLabel = null!, StatusLabel = null!;
    SpinBox TicketsInput = null!;
    Button LoadButton = null!, Add100Button = null!, Add1000Button = null!, SaveButton = null!, ResetBoxesButton = null!;

    public override void _Ready()
    {
        DataManager = DataManager.Get(this);

        ProfileLabel = GetNode<Label>($"%{nameof(ProfileLabel)}");
        StatusLabel = GetNode<Label>($"%{nameof(StatusLabel)}");
        TicketsInput = GetNode<SpinBox>($"%{nameof(TicketsInput)}");
        LoadButton = GetNode<Button>($"%{nameof(LoadButton)}");
        Add100Button = GetNode<Button>($"%{nameof(Add100Button)}");
        Add1000Button = GetNode<Button>($"%{nameof(Add1000Button)}");
        SaveButton = GetNode<Button>($"%{nameof(SaveButton)}");
        ResetBoxesButton = GetNode<Button>($"%{nameof(ResetBoxesButton)}");

        LoadButton.Pressed += ShowSlots;
        Add100Button.Pressed += () => TicketsInput.Value += 100;
        Add1000Button.Pressed += () => TicketsInput.Value += 1000;
        SaveButton.Pressed += SaveTickets;
        ResetBoxesButton.Pressed += ResetBoxes;

        Render();
    }

    private void ShowSlots()
    {
        var dialog = RevGodotCoreSceneFactory.CreateSlotSelectorDialog(DataManager.GetProfileSlots(), SlotSelectorMode.LOAD);
        dialog.Theme = GD.Load<Theme>("res://Resources/Themes/GUITheme.tres");
        dialog.MinSize = new Vector2I(900, 800);
        dialog.SlotSelected += slot =>
        {
            DataManager.LoadProfile(slot);
            SetStatus($"Loaded slot {slot.Number}");
            Render();
        };
        dialog.VisibilityChanged += () =>
        {
            if(!dialog.Visible) dialog.QueueFree();
        };
        AddChild(dialog);
        dialog.PopupCentered();
    }

    private void SaveTickets()
    {
        var profile = DataManager.CurrentProfile;
        if(profile is null) return;

        profile.Resources.Tickets = (int)TicketsInput.Value;
        DataManager.SaveProfile();
        SetStatus($"Saved {profile.Resources.Tickets} tickets");
    }

    private void ResetBoxes()
    {
        var profile = DataManager.CurrentProfile;
        if(profile is null) return;

        int boxes = profile.GachaBoxes.Count;
        profile.GachaBoxes.Clear();
        DataManager.SaveProfile();
        SetStatus($"Removed {boxes} gacha boxes, the next pull rolls new ones");
    }

    private void Render()
    {
        var profile = DataManager.CurrentProfile;
        bool loaded = profile is not null;
        ProfileLabel.Text = loaded ? $"{profile!.Name} (slot {DataManager.CurrentSlot!.Number})" : "No profile, load one";
        TicketsInput.Value = profile?.Resources.Tickets ?? 0;
        TicketsInput.Editable = loaded;
        Add100Button.Disabled = Add1000Button.Disabled = SaveButton.Disabled = ResetBoxesButton.Disabled = !loaded;
    }

    private void SetStatus(string status) => StatusLabel.Text = status;
}
