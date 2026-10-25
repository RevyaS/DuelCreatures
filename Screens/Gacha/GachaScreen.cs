using System.Collections.Generic;
using Godot;

/// <summary>Pick a set, then a pack out of its box. Opening a pack hands it to <see cref="PackOpening"/>.</summary>
public partial class GachaScreen : Control
{
    static readonly Vector2 PackButtonSize = new(200, 330);

    DataManager DataManager = null!;
    GachaSet? CurrentSet;

    Label Tickets = null!, Title = null!, BoxInfo = null!, StatusLabel = null!;
    Control SetsView = null!, BoxView = null!;
    VBoxContainer SetList = null!;
    TextureRect BoxImage = null!;
    GridContainer PackGrid = null!;
    Button BackButton = null!;
    PackOpening Opening = null!;

    public override void _Ready()
    {
        DataManager = DataManager.Get(this);

        Tickets = GetNode<Label>($"%{nameof(Tickets)}");
        Title = GetNode<Label>($"%{nameof(Title)}");
        BoxInfo = GetNode<Label>($"%{nameof(BoxInfo)}");
        StatusLabel = GetNode<Label>($"%{nameof(StatusLabel)}");
        SetsView = GetNode<Control>($"%{nameof(SetsView)}");
        BoxView = GetNode<Control>($"%{nameof(BoxView)}");
        SetList = GetNode<VBoxContainer>($"%{nameof(SetList)}");
        BoxImage = GetNode<TextureRect>($"%{nameof(BoxImage)}");
        PackGrid = GetNode<GridContainer>($"%{nameof(PackGrid)}");
        BackButton = GetNode<Button>($"%{nameof(BackButton)}");
        Opening = GetNode<PackOpening>($"%{nameof(Opening)}");

        BackButton.Pressed += BackPressed;
        Opening.Finished += () =>
        {
            if(CurrentSet is not null) ShowBox(CurrentSet);
        };
        DataManager.ProfileChanged += RenderTickets;

        RenderTickets();
        ShowSets();
    }

    public override void _ExitTree()
    {
        DataManager.ProfileChanged -= RenderTickets;
    }

    private void RenderTickets()
    {
        Tickets.Text = $"Tickets: {DataManager.CurrentProfile?.Resources.Tickets ?? 0}";
    }

    private void ShowSets()
    {
        CurrentSet = null;
        Title.Text = "Gacha";
        StatusLabel.Text = string.Empty;
        SetsView.Show();
        BoxView.Hide();

        SetList.FreeChildren();
        // Missing images only get placeholders, anything else makes the set unusable
        var problems = DataManager.ValidateGachaSets(checkImages: false);
        foreach(var file in DataManager.GachaSetFiles)
        {
            var set = file.Set;
            var fileProblems = problems.GetValueOrDefault(file.Path);
            var button = new Button
            {
                Text = set is null ? file.Path.GetFile() : $"{set.Name}  ({set.TicketCost} tickets per pack)",
                Disabled = set is null || fileProblems is not null,
                TooltipText = fileProblems is null ? string.Empty : string.Join("\n", fileProblems),
                CustomMinimumSize = new Vector2(0, 100)
            };
            button.AddThemeFontSizeOverride("font_size", 36);
            if(set is not null) button.Pressed += () => ShowBox(set);
            SetList.AddChild(button);

            if(fileProblems is not null)
            {
                GD.PushWarning($"Gacha set {file.Path}: {string.Join(" | ", fileProblems)}");
            }
        }

        if(DataManager.GachaSetFiles.Count == 0)
        {
            StatusLabel.Text = $"No sets in {GachaConfig.SetsDirectory}";
        }
    }

    private void ShowBox(GachaSet set)
    {
        bool sameSet = CurrentSet == set;
        CurrentSet = set;
        SetsView.Hide();
        BoxView.Show();

        int packsLeftBefore = DataManager.CurrentProfile!.GachaBoxes.GetValueOrDefault(set.Id)?.PacksLeft ?? -1;
        var box = DataManager.GetGachaBox(set);
        if(sameSet && packsLeftBefore == 0) StatusLabel.Text = "The box is empty, here's a new one!";
        else if(!sameSet) StatusLabel.Text = string.Empty;

        Title.Text = set.Name;
        BoxImage.Texture = LoadTexture(set.BoxImage);
        BoxInfo.Text = $"{box.PacksLeft}/{box.Packs.Count} packs left - {set.TicketCost} tickets per pack";

        PackGrid.FreeChildren();
        for(int i = 0; i < box.Packs.Count; i++)
        {
            int packIndex = i;
            bool opened = box.Packs[i].Opened;
            var texture = LoadTexture(set.GetPackImage(i));
            var button = new Button
            {
                CustomMinimumSize = PackButtonSize,
                Icon = opened ? null : texture,
                ExpandIcon = true,
                IconAlignment = HorizontalAlignment.Center,
                Text = opened ? "Opened" : texture is null ? $"Pack {i + 1}" : string.Empty,
                Disabled = opened
            };
            button.Pressed += () => OpenPack(set, box, packIndex);
            PackGrid.AddChild(button);
        }
    }

    private void OpenPack(GachaSet set, GachaBox box, int packIndex)
    {
        if(GachaService.CanOpen(DataManager.CurrentProfile!, set, box, packIndex) is string problem)
        {
            StatusLabel.Text = problem;
            return;
        }
        StatusLabel.Text = string.Empty;

        var result = DataManager.OpenPack(set, packIndex);
        Opening.Open(result, LoadTexture(set.GetPackImage(packIndex)));
    }

    private void BackPressed()
    {
        if(BoxView.Visible) ShowSets();
        else GUIManager.Get(this).OpenMainScreen();
    }

    private static Texture2D? LoadTexture(string path) =>
        !string.IsNullOrEmpty(path) && ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
}
