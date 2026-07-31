using BusinessCloud.Application.Bazares.Commands.DeletePackedOrderPhoto;
using BusinessCloud.Application.Bazares.Commands.UploadPackedOrderPhotos;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Tests.TestSupport;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BusinessCloud.Tests.Application.Bazares;

public class PackedOrderPhotosHandlerTests
{
    private const string Tenant = BazaresContextFactory.TenantId;

    [Fact]
    public async Task UploadAndDelete_MultiplePhotos_PersistsAndRemovesPhysicalBlob()
    {
        using var context = BazaresContextFactory.Create();
        context.ClosureCustomerTotals.Add(new BzaClosureCustomerTotal
        {
            Id = 1,
            TenantId = Tenant,
            BzaCustomerId = 1,
            BzaClosureEventId = 1,
            UploadToken = "packed-token",
            Customer = new BzaCustomer { Id = 1, TenantId = Tenant, Name = "Ana", Phone = "5511112222" },
            ClosureEvent = new BzaClosureEvent
            {
                Id = 1,
                TenantId = Tenant,
                Description = "Cierre",
                Items = []
            },
            PackedOrderPhotos = []
        });
        await context.SaveChangesAsync();

        var blob = new Mock<IBlobStorageService>();
        blob.SetupSequence(service => service.UploadAsync(
                "bazarez",
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://files.example/packed-1.jpg")
            .ReturnsAsync("https://files.example/packed-2.png");
        blob.Setup(service => service.DeleteAsync(
                "bazarez",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var upload = new UploadPackedOrderPhotosHandler(context, blob.Object);
        var uploadResult = await upload.Handle(new UploadPackedOrderPhotosCommand(1,
        [
            new(new MemoryStream([1, 2]), "pedido-1.jpg", "image/jpeg"),
            new(new MemoryStream([3, 4]), "pedido-2.png", "image/png")
        ]), default);

        Assert.True(uploadResult.Success);
        Assert.Equal(2, uploadResult.Photos.Count);
        Assert.Equal(2, await context.PackedOrderPhotos.CountAsync());

        var photo = await context.PackedOrderPhotos.OrderBy(item => item.Id).FirstAsync();
        var delete = new DeletePackedOrderPhotoHandler(context, blob.Object);
        var deleteResult = await delete.Handle(new DeletePackedOrderPhotoCommand(photo.Id), default);

        Assert.True(deleteResult.Success);
        Assert.Equal(1, deleteResult.RemainingPhotos);
        Assert.Single(await context.PackedOrderPhotos.ToListAsync());
        blob.Verify(service => service.DeleteAsync("bazarez", photo.BlobName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Validator_RejectsUnsupportedOrOversizedPhoto()
    {
        var validator = new UploadPackedOrderPhotosValidator();
        var command = new UploadPackedOrderPhotosCommand(1,
        [
            new(new MemoryStream(new byte[15_000_001]), "pedido.gif", "image/gif")
        ]);

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Files[0].Content");
        result.ShouldHaveValidationErrorFor("Files[0].ContentType");
    }
}