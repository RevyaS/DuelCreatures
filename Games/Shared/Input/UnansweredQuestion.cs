using System;

/// <summary>A question ArC asked that has no gesture yet: it names what was asked, so the gesture can be designed for it.</summary>
public sealed class UnansweredQuestion(object? purpose, string prompt, Type type, string? detail = null)
    : Exception($"No gesture answers \"{prompt}\" (purpose {purpose ?? "none"}, choosing {type.Name}){(detail is null ? "" : $": {detail}")}");
