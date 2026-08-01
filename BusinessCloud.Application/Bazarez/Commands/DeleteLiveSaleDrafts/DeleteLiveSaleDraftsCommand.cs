using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.DeleteLiveSaleDrafts;

public record DeleteLiveSaleDraftsCommand(int BzaEventId, int? DraftId = null) : IRequest<int>;
