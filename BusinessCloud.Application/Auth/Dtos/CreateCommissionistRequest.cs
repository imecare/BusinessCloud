namespace BusinessCloud.Application.Auth.Dtos;

public class CreateCommissionistRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public required int SellerId { get; set; }
}