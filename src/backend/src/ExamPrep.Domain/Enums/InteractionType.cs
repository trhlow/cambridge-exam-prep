namespace ExamPrep.Domain.Enums;

/// <summary>How the candidate answers the questions of a group.</summary>
public enum InteractionType
{
    /// <summary>Pick one text option (radio).</summary>
    SingleChoice,

    /// <summary>Pick one picture option (radio under each picture).</summary>
    ImageChoice,

    /// <summary>Gap inside a text; clicking it shows the word options.</summary>
    InlineChoice,

    /// <summary>Gap inside a text or form; the candidate types the answer.</summary>
    InlineText,

    /// <summary>Drag a word card from a shared pool onto a numbered slot.</summary>
    DragDropMatch,

    /// <summary>Free writing with a word counter.</summary>
    Essay,
}
