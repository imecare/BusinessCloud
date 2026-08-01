using BusinessCloud.Application.Bazares.Commands.CommitBzaImport;
using Xunit;

namespace BusinessCloud.Tests.Application.Bazares;

public class CommitBzaImportValidatorTests
{
    private static CommitBzaImportCommand Command(string facebookName, List<CommitNewCollectorDto>? collectors = null)
        => new(
            1,
            false,
            collectors ?? [],
            [new CommitImportCustomerDto
            {
                NewCustomer = new CommitImportNewCustomerDto
                {
                    Name = "Cliente",
                    HasNoWhatsApp = true,
                    CollectorName = "Recolector",
                    FacebookName = facebookName,
                },
                Products = [new CommitImportProductDto { Description = "Producto", Price = 100m }],
            }]);

    [Fact]
    public void Validate_FacebookSinUsuario_EsInvalido()
    {
        var result = new CommitBzaImportValidator().Validate(Command("https://www.facebook.com/"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName.Contains("FacebookName"));
    }

    [Fact]
    public void Validate_FacebookConUsuario_EsValido()
    {
        var result = new CommitBzaImportValidator().Validate(Command("https://www.facebook.com/cliente.nuevo"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RecolectorManualSinGrupo_EsInvalido()
    {
        var collectors = new List<CommitNewCollectorDto> { new() { Name = "Recolector nuevo", GroupId = 0 } };

        var result = new CommitBzaImportValidator().Validate(Command("https://www.facebook.com/cliente", collectors));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName.Contains("GroupId"));
    }
}
