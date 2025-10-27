using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LibHub.CatalogService.Application.DTOs;
using LibHub.CatalogService.Application.Services;

namespace LibHub.CatalogService.Api.Controllers;

[ApiController]
[Route("api/books")]
public class BooksController : ControllerBase
{
    private readonly BookApplicationService _bookService;
    private readonly ILogger<BooksController> _logger;

    public BooksController(
        BookApplicationService bookService,
        ILogger<BooksController> logger)
    {
        _bookService = bookService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<BookDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBooks(
        [FromQuery] string? search, 
        [FromQuery] string? genre)
    {
        try
        {
            var books = await _bookService.SearchBooksAsync(search, genre);
            return Ok(books);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching books");
            return StatusCode(500, new { message = "An error occurred while searching books" });
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookById(int id)
    {
        try
        {
            var book = await _bookService.GetBookByIdAsync(id);
            return Ok(book);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Book not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving book {BookId}", id);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateBook([FromBody] CreateBookDto dto)
    {
        try
        {
            var book = await _bookService.CreateBookAsync(dto);
            _logger.LogInformation("Book created: {BookId} - {Title}", book.BookId, book.Title);
            
            return CreatedAtAction(nameof(GetBookById), new { id = book.BookId }, book);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Book creation validation failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Book creation failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating book");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateBook(int id, [FromBody] UpdateBookDto dto)
    {
        try
        {
            await _bookService.UpdateBookAsync(id, dto);
            _logger.LogInformation("Book updated: {BookId}", id);
            
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Book not found" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating book {BookId}", id);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteBook(int id)
    {
        try
        {
            await _bookService.DeleteBookAsync(id);
            _logger.LogInformation("Book deleted: {BookId}", id);
            
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Book not found" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cannot delete book {BookId}: {Message}", id, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting book {BookId}", id);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    [HttpPut("{id}/stock")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockDto dto)
    {
        try
        {
            await _bookService.UpdateStockAsync(id, dto);
            _logger.LogInformation("Stock updated for book {BookId}: {ChangeAmount}", id, dto.ChangeAmount);
            
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Book not found" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Stock update failed for book {BookId}: {Message}", id, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock for book {BookId}", id);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }
}
