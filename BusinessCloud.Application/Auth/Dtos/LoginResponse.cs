public class LoginResponse
{
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string Name { get; set; } = null!;
}
