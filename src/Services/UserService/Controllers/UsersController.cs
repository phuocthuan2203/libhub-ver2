using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LibHub.UserService.Models.Requests;
using LibHub.UserService.Models.Responses;

namespace LibHub.UserService.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly LibHub.UserService.Services.UserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        LibHub.UserService.Services.UserService userService,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            _logger.LogInformation(
                "📝 [REGISTER-ATTEMPT] Registration attempt | Email: {Email}", 
                request.Email);
            
            var user = await _userService.RegisterAsync(request);
            
            _logger.LogInformation(
                "✅ [REGISTER-SUCCESS] User registered successfully | Email: {Email} | UserId: {UserId}", 
                user.Email, user.UserId);
            
            return CreatedAtAction(nameof(GetUserById), new { id = user.UserId }, user);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(
                "❌ [REGISTER-FAILED] Validation error | Email: {Email} | Reason: {Reason}", 
                request.Email, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                "❌ [REGISTER-FAILED] Registration failed | Email: {Email} | Reason: {Reason}", 
                request.Email, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "❌ [REGISTER-ERROR] Registration error | Email: {Email}", 
                request.Email);
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation(
                "🔐 [LOGIN-ATTEMPT] Login attempt | Email: {Email}", 
                request.Email);
            
            var token = await _userService.LoginAsync(request);
            
            _logger.LogInformation(
                "✅ [LOGIN-SUCCESS] User logged in successfully | Email: {Email}", 
                request.Email);
            
            return Ok(token);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                "❌ [LOGIN-FAILED] Invalid credentials | Email: {Email} | Reason: {Reason}", 
                request.Email, ex.Message);
            return Unauthorized(new { message = "Invalid email or password" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "❌ [LOGIN-ERROR] Login error | Email: {Email}", 
                request.Email);
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        
        if (user == null)
            return NotFound(new { message = "User not found" });
        
        return Ok(user);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized();

        var userId = int.Parse(userIdClaim.Value);
        var user = await _userService.GetByIdAsync(userId);
        
        if (user == null)
            return NotFound(new { message = "User not found" });
        
        return Ok(user);
    }
}

