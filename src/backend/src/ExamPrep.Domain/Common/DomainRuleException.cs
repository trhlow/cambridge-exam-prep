namespace ExamPrep.Domain.Common;

/// <summary>An operation is not allowed in the entity's current state (e.g. pausing a submitted attempt).</summary>
public sealed class DomainRuleException(string message) : InvalidOperationException(message);
