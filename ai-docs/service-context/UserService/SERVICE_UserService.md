# UserService - Clean Architecture Design

**Domain**: Identity & Access Management  
**Port**: 5002  
**Database**: user_db  
**Responsibilities**: User registration, authentication, JWT token generation

---

## Clean Architecture Layers

### 1. Domain Layer (`LibHub.UserService.Domain`)

**Zero external dependencies - pure C# only**

#### User Entity (Aggregate Root)
```
public class User
{
    public int UserId { get; private set; }
    public string Username { get; private set; }
    public string Email { get; private set; }
    public string HashedPassword { get; private set; }
    public string Role { get; private set; }  // "Customer" or "Admin"
    public DateTime CreatedAt { get; private set; }
    
    public User(string username, string email, string plainPassword, string role)
    {
        // Validates: username not empty, email format, role valid
        // Hashes password with BCrypt (work factor 11)
        Username = username;
        Email = email.ToLowerInvariant();
        HashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword, 11);
        Role = role;
        CreatedAt = DateTime.UtcNow;
    }
    
    public bool VerifyPassword(string plainPassword)
    {
        return BCrypt.Net.BCrypt.Verify(plainPassword, HashedPassword);
    }
}
```

#### Repository Interface
```
public interface IUserRepository
{
    Task<User> GetByIdAsync(int userId);
    Task<User> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
}
```

---

### 2. Application Layer (`LibHub.UserService.Application`)

**Depends on Domain Layer only**

#### DTOs
```
public class RegisterUserDto
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}

public class LoginDto
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class UserDto
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
}

public class TokenDto
{
    public string AccessToken { get; set; }
    public int ExpiresIn { get; set; }
}
```

#### Application Service
```
public class IdentityApplicationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    
    public async Task<UserDto> RegisterUserAsync(RegisterUserDto dto)
    {
        // 1. Check email exists
        if (await _userRepository.EmailExistsAsync(dto.Email))
            throw new Exception("Email already exists");
        
        // 2. Create User entity (password hashed in constructor)
        var user = new User(dto.Username, dto.Email, dto.Password, "Customer");
        
        // 3. Persist
        await _userRepository.AddAsync(user);
        
        // 4. Return DTO
        return new UserDto { UserId = user.UserId, Username = user.Username, Email = user.Email, Role = user.Role };
    }
    
    public async Task<TokenDto> LoginUserAsync(LoginDto dto)
    {
        // 1. Get user
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null) throw new Exception("Invalid credentials");
        
        // 2. Verify password
        if (!user.VerifyPassword(dto.Password))
            throw new Exception("Invalid credentials");
        
        // 3. Generate JWT
        var token = _jwtTokenGenerator.GenerateToken(user);
        
        return new TokenDto { AccessToken = token, ExpiresIn = 3600 };
    }
}
```

#### Infrastructure Interfaces (defined in Application)
```
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hash, string password);
}

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
```

---

### 3. Infrastructure Layer (`LibHub.UserService.Infrastructure`)

**Implements interfaces from Domain and Application layers**

#### EfUserRepository
```
public class EfUserRepository : IUserRepository
{
    private readonly UserDbContext _context;
    
    public async Task<User> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
    }
    
    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }
    
    // Other methods...
}
```

#### PasswordHasher Implementation
```
public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password) 
        => BCrypt.Net.BCrypt.HashPassword(password, 11);
    
    public bool Verify(string hash, string password) 
        => BCrypt.Net.BCrypt.Verify(password, hash);
}
```

#### JwtTokenGenerator Implementation
```
public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;
    
    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
        
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };
        
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

#### DbContext
```
public class UserDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.HashedPassword).IsRequired();
            entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
        });
    }
}
```

---

### 4. Presentation Layer (`LibHub.UserService.Api`)

**Thin controllers - delegate to Application Layer**

#### UsersController
```
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IdentityApplicationService _identityService;
    
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserDto dto)
    {
        var user = await _identityService.RegisterUserAsync(dto);
        return CreatedAtAction(nameof(GetUserById), new { id = user.UserId }, user);
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var token = await _identityService.LoginUserAsync(dto);
        return Ok(token);
    }
    
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetUserById(int id)
    {
        // Implementation
    }
}
```

#### Dependency Injection (Program.cs)
```
// Database
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

// Repositories
builder.Services.AddScoped<IUserRepository, EfUserRepository>();

// Application Services
builder.Services.AddScoped<IdentityApplicationService>();

// Infrastructure Services
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
        };
    });
```

---

## Required NuGet Packages

```
<!-- Domain (no packages) -->

<!-- Application (no packages) -->

<!-- Infrastructure -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.*" />
<PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="8.0.*" />
<PackageReference Include="BCrypt.Net-Next" Version="4.0.*" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.0.*" />

<!-- Presentation -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.*" />
```

---

## Key Implementation Notes

1. **Password Security**: Always hash with BCrypt (work factor 11), never store plaintext
2. **Email Storage**: Store lowercase for case-insensitive lookups
3. **JWT Claims**: Include UserId, Email, and Role
4. **Token Expiry**: 1 hour (3600 seconds)
5. **Error Handling**: Use try-catch in controllers, return appropriate HTTP status codes
6. **Validation**: Implement FluentValidation for DTOs (optional but recommended)
7. **Logging**: Log all authentication events (success and failure)

