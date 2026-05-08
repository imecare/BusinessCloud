using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.SignDispatchSheet;

public class SignDispatchSheetHandler : IRequestHandler<SignDispatchSheetCommand>
{
    private readonly IBazaresDbContext _context;
    private readonly IMongoContext _mongoContext;

    public SignDispatchSheetHandler(IBazaresDbContext context, IMongoContext mongoContext)
    {
        _context = context;
        _mongoContext = mongoContext;
    }

    public async Task Handle(SignDispatchSheetCommand request, CancellationToken ct)
    {
        var sheet = await _context.DispatchSheets
            .FirstOrDefaultAsync(s => s.Id == request.DispatchSheetId, ct)
            ?? throw new KeyNotFoundException("Hoja de despacho no encontrada.");

        if (sheet.Status == 2)
            throw new InvalidOperationException("Esta hoja ya fue firmada.");

        sheet.CollectorSignatureUrl = request.SignatureBase64;
        sheet.SignedAt = DateTime.UtcNow;
        sheet.Status = 2;

        await _context.SaveChangesAsync(ct);

        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_DispatchSigned",
            SheetId = sheet.Id,
            SignedAt = sheet.SignedAt,
            Timestamp = DateTime.UtcNow
        }, ct);
    }
}
