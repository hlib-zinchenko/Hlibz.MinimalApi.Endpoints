using System.Collections.Concurrent;

namespace Library.Api.Books;

public sealed record Book(int Id, string Title, string Author);

/// <summary>
/// An in-memory catalog standing in for a database.
/// </summary>
public sealed class BookStore
{
    private readonly ConcurrentDictionary<int, Book> _books = new()
    {
        [1] = new Book(1, "Dune", "Frank Herbert"),
        [2] = new Book(2, "Hyperion", "Dan Simmons"),
    };

    private int _lastId = 2;

    public IEnumerable<Book> All => _books.Values.OrderBy(book => book.Id);

    public Book? Find(int id) => _books.GetValueOrDefault(id);

    public Book Add(string title, string author)
    {
        Book book = new(Interlocked.Increment(ref _lastId), title, author);
        _books[book.Id] = book;
        return book;
    }

    public bool Remove(int id) => _books.TryRemove(id, out _);
}
