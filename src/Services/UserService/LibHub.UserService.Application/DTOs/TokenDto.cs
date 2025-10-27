namespace LibHub.UserService.Application.DTOs;

public class TokenDto
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
}
