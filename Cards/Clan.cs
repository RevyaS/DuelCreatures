using System.Text.RegularExpressions;

/// <summary>A clan is a nested class of a card factory, <see cref="Id"/> is that class's name (eg; RoyalPaladin).</summary>
public sealed record Clan(string Id)
{
    public string DisplayName => Regex.Replace(Id, "(?<=[a-z])(?=[A-Z])", " ");

    public override string ToString() => DisplayName;
}
