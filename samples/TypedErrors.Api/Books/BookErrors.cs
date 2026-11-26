using Hlibz.Results;

namespace TypedErrors.Api.Books;

// The failures the handlers declare, each with the data a client needs to act on it. Clients
// branch on the code; the message is for logs and developers.

/// <summary>No book has the id.</summary>
/// <param name="BookId">The id asked for.</param>
[Error("library_1001", ResultStatus.NotFound, "The book doesn't exist.")]
public sealed record BookNotFound(int BookId) : Error;

/// <summary>The catalog already has the book.</summary>
/// <param name="BookId">The id of the book already in the catalog.</param>
[Error("library_1002", ResultStatus.Conflict, "The catalog already has a book with this title by this author.")]
public sealed record BookExists(int BookId) : Error;

/// <summary>The book is on loan, so it can't be removed.</summary>
/// <param name="Borrowers">Who has it.</param>
[Error("library_1003", ResultStatus.Conflict, "The book is on loan.")]
public sealed record BookOnLoan(IReadOnlyList<string> Borrowers) : Error;
