using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.DeleteDeliveryProof;

public class DeleteDeliveryProofHandler(IBazaresDbContext context)
    : IRequestHandler<DeleteDeliveryProofCommand, DeleteDeliveryProofResultDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<DeleteDeliveryProofResultDto> Handle(DeleteDeliveryProofCommand request, CancellationToken cancellationToken)
    {
        var proof = await _context.ClosureDeliveryProofs
            .FirstOrDefaultAsync(p => p.Id == request.ProofId, cancellationToken)
            ?? throw new KeyNotFoundException("El comprobante de entrega no existe.");

        _context.ClosureDeliveryProofs.Remove(proof);
        await _context.SaveChangesAsync(cancellationToken);

        return new DeleteDeliveryProofResultDto { Success = true };
    }
}