using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.DeleteBzaSale;

public record DeleteBzaSaleCommand(int Id, string? Reason) : IRequest<DeleteBzaSaleResult>;

public record DeleteBzaSaleResult(bool Success, string Message);
