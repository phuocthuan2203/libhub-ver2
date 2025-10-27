namespace LibHub.CatalogService.Domain;

public interface IBookRepository
{
    Task<Book?> GetByIdAsync(int bookId);
    
    Task<Book?> GetByIsbnAsync(string isbn);
    
    Task<List<Book>> SearchAsync(string? searchTerm = null, string? genre = null);
    
    Task<List<Book>> GetAllAsync();
    
    Task AddAsync(Book book);
    
    Task UpdateAsync(Book book);
    
    Task DeleteAsync(int bookId);
}
