using System.Collections.Concurrent;

namespace TypedErrors.Api.Books;

public sealed record Book(int Id, string Title, string Author);

/// <summary>
/// An in-memory catalog standing in for a database. Dune is on loan.
/// </summary>
public sealed class BookStore
{
    private readonly ConcurrentDictionary<int, Book> _books = new()
    {
        [1] = new Book(1, "Dune", "Frank Herbert"),
        [2] = new Book(2, "Hyperion", "Dan Simmons"),
    };

    private readonly ConcurrentDictionary<int, string[]> _loans = new() { [1] = ["alice", "bob"] };

    private int _lastId = 2;

    public Book? Find(int id) => _books.GetValueOrDefault(id);

    public Book? Find(string title, string author) =>
        _books.Values.FirstOrDefault(book =>
            string.Equals(book.Title, title, StringComparison.OrdinalIgnoreCase)
            && string.Equals(book.Author, author, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<string> BorrowersOf(int id) => _loans.GetValueOrDefault(id) ?? [];

    public Book Add(string title, string author)
    {
        Book book = new(Interlocked.Increment(ref _lastId), title, author);
        _books[book.Id] = book;
        return book;
    }

    public void Remove(int id) => _books.TryRemove(id, out _);
}
