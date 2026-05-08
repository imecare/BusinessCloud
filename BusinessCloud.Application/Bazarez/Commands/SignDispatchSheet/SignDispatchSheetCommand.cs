using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.SignDispatchSheet;

public record SignDispatchSheetCommand(int DispatchSheetId, string SignatureBase64) : IRequest;
