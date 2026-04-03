using MediatR;
using System;

namespace BusinessCloud.Application.Commissions.Commands.PayCommissions;

// Comando para liquidar comisiones de un vendedor específico en un rango de fechas
public record PayCommissionsCommand(int SellerId, DateTime ToDate) : IRequest<int>;