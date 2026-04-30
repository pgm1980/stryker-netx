namespace Stryker.Core.Mutants;

/// <summary>
/// This enum is used to track the syntax 'level' of mutations that are injected in the code.
/// </summary>
public enum MutationControl
{
    /// <summary>
    /// Syntax that is part of a member access expression (such as class.Property.Property.Invoke())
    /// </summary>
    MemberAccess,
    /// <summary>
    /// Syntax that is part of an expression (80-90% of syntax is expression)
    /// </summary>
    Expression,
    /// <summary>
    /// Statements
    /// </summary>
    Statement,
    /// <summary>
    /// Block of Statement
    /// </summary>
    Block,
    /// <summary>
    /// Class member or equivalent, there is no higher (supported) syntax structure
    /// </summary>
    Member
}
