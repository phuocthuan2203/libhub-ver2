using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LibHub.UserService.Application.DTOs;
using LibHub.UserService.Application.Services;

namespace LibHub.UserService.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IdentityApplicationService _identityService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IdentityApplicationService identityService,
        ILogger<UsersController> logger)
    {
        _identityService = identityService;
        _logger = logger;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        try
        {
            var user = await _identityService.RegisterUserAsync(dto);
            _logger.LogInformation("User registered: {Email}", user.Email);
            
            return CreatedAtAction(nameof(GetUserById), new { id = user.UserId }, user);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Registration validation failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Registration failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error");
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var token = await _identityService.LoginUserAsync(dto);
            _logger.LogInformation("User logged in: {Email}", dto.Email);
            
            return Ok(token);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Login failed: {Message}", ex.Message);
            return Unauthorized(new { message = "Invalid email or password" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error");
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _identityService.GetUserByIdAsync(id);
        
        if (user == null)
            return NotFound(new { message = "User not found" });
        
        return Ok(user);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized();

        var userId = int.Parse(userIdClaim.Value);
        var user = await _identityService.GetUserByIdAsync(userId);
        
        if (user == null)
            return NotFound(new { message = "User not found" });
        
        return Ok(user);
    }
}
