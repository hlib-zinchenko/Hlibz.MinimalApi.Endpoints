namespace TypedErrors.Api.Books;

// Each endpoint's errors, as a union.
public union GetBookError(BookNotFound);

public union AddBookError(BookExists);

public union DeleteBookError(BookNotFound, BookOnLoan);
