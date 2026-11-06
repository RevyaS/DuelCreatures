using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// One question ArC asks a player (e.g. "a card from the hand"), typed by its answer: a strategy can only pre-answer it
/// with the right type of answer. A game declares its questions once (e.g. YuGiOhQuestions).
/// </summary>
public sealed class Question<TAnswer>(string name)
{
    public string Name => name;
    public override string ToString() => name;
}

/// <summary>
/// Answers a strategy already knows from a gesture, for the questions the game is about to ask (a card dropped on a
/// zone answers "which card" and "which zone"). The input provider gives them before asking anyone; one never asked
/// for is an error (<see cref="EnsureNoneLeft"/>): the action asked different questions than the strategy expected.
/// </summary>
public sealed class PreAnswers
{
    readonly Dictionary<object, object?> answers = [];

    public PreAnswers Set<TAnswer>(Question<TAnswer> question, TAnswer answer)
    {
        if(answers.ContainsKey(question)) throw new InvalidOperationException($"\"{question}\" is already answered");
        answers[question] = answer;
        return this;
    }

    public bool TryTake<TAnswer>(Question<TAnswer> question, out TAnswer answer)
    {
        if(answers.Remove(question, out var found))
        {
            answer = (TAnswer)found!;
            return true;
        }
        answer = default!;
        return false;
    }

    /// <summary>Fails, listing them, when answers are left that nothing asked for (and drops them).</summary>
    public void EnsureNoneLeft(string when)
    {
        if(answers.Count == 0) return;
        var left = string.Join(", ", answers.Keys);
        answers.Clear();
        throw new InvalidOperationException($"Answers given but never asked for ({left}) {when}: the action asked different questions than its strategy expected");
    }
}
