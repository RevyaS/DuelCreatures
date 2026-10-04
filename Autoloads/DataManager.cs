using Godot;
using MessagePack;

public partial class DataManager : Node
{
    const string SavePath = "user://Save.dat";
    const string DefaultSave = "res://Data/InitialData/Save.dat";
    const string AIDeckListPath = "res://Data/InitialData/AIDecks.dat";

    // AI Data
    public DeckList AIDecks { get; private set; } = null!;
    public DeckList PlayerDecks { get; private set; } = null!;

    public override void _Ready()
    {
        EnsureSaveData();
        LoadPlayerDecks();
        LoadAIDecks();
    }

    private void EnsureSaveData()
    {
        if(FileAccess.FileExists(SavePath))
        {
            using var src = FileAccess.Open(DefaultSave, FileAccess.ModeFlags.Read);
            using var dst = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);

            byte[] buffer = src.GetBuffer((long)src.GetLength());
            dst.StoreBuffer(buffer);
        }
    }

    private void LoadAIDecks()
    {
        using var file = FileAccess.Open(AIDeckListPath, FileAccess.ModeFlags.Read);
        byte[] data = file.GetBuffer((long)file.GetLength());
        AIDecks = MessagePackSerializer.Deserialize<DeckList>(data);
    }

    private void LoadPlayerDecks()
    {
        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        byte[] data = file.GetBuffer((long)file.GetLength());
        PlayerDecks = MessagePackSerializer.Deserialize<DeckList>(data);
    }
}