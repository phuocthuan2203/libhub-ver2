using Microsoft.EntityFrameworkCore;
using LibHub.CatalogService.Models.Entities;

namespace LibHub.CatalogService.Data;

public class BookRepository
{
    private readonly CatalogDbContext _context;

    public BookRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<Book?> GetByIdAsync(int bookId)
    {
        return await _context.Books.FindAsync(bookId);
    }

    public async Task<Book?> GetByIsbnAsync(string isbn)
    {
        return await _context.Books
            .FirstOrDefaultAsync(b => b.Isbn == isbn);
    }

    public async Task<List<Book>> SearchAsync(string? searchTerm = null, string? genre = null)
    {
        var query = _context.Books.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(b =>
                b.Title.ToLower().Contains(searchTerm) ||
                b.Author.ToLower().Contains(searchTerm) ||
                b.Isbn.Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(genre))
        {
            query = query.Where(b => b.Genre == genre);
        }

        return await query.ToListAsync();
    }

    public async Task<List<Book>> GetAllAsync()
    {
        return await _context.Books.ToListAsync();
    }

    public async Task AddAsync(Book book)
    {
        await _context.Books.AddAsync(book);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Book book)
    {
        _context.Books.Update(book);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int bookId)
    {
        var book = await GetByIdAsync(bookId);
        if (book != null)
        {
            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
        }
    }
}

