namespace BusinessCloud.Application.Payments.Dtos;

public class SellerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int StatusId { get; set; }
}
