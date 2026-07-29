using BusinessCloud.Application.Common.Utilities;
using Xunit;

namespace BusinessCloud.Tests.Application.Common;

public class PhoneNumberCandidatesTests
{
    [Theory]
    [InlineData("5213121232192")] // 521 + 10 (movil MX que envia WhatsApp)
    [InlineData("523121232192")]  // 52 + 10
    [InlineData("3121232192")]    // 10 digitos
    [InlineData("+52 1 312 123 2192")] // con simbolos y espacios
    public void Build_NumeroMexicano_IncluyeNucleoDe10Digitos(string phone)
    {
        var candidates = PhoneNumberCandidates.Build(phone);

        Assert.Contains("3121232192", candidates);
        Assert.Contains("523121232192", candidates);
        Assert.Contains("5213121232192", candidates);
    }

    [Fact]
    public void Build_Vacio_RegresaListaVacia()
    {
        Assert.Empty(PhoneNumberCandidates.Build(""));
        Assert.Empty(PhoneNumberCandidates.Build(null));
    }
}