using MediatR;
using System.Collections.Generic;

namespace BusinessCloud.Application.Payments.Queries.GetCustomerHistory;

public record GetCustomerHistoryQuery(
    string CustomerPhone,
    string? CustomerRFC,
    string? TenantId = null // Propiedad faltante
) : IRequest<List<CustomerHistoryDto>>;