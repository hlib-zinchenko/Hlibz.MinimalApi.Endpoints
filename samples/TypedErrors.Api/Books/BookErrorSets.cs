namespace TypedErrors.Api.Books;

// Each endpoint's errors, as a union. A union compiles to a struct without equality, which
// CA1815 flags.
#pragma warning disable CA1815

public union GetBookError(BookNotFound);

public union AddBookError(BookExists);

public union DeleteBookError(BookNotFound, BookOnLoan);
