using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.DeleteClosureDraft;

/// <summary>
/// Elimina un cierre draft para permitir regenerar el env?o de totales con nuevos datos.
/// </summary>
public record DeleteClosureDraftCommand(int ClosureEventId) : IRequest;
