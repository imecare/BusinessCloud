using BusinessCloud.Application.Company.Dtos;
using MediatR;

namespace BusinessCloud.Application.Company.Queries.GetCompanyContext;

public record GetCompanyContextQuery : IRequest<CompanyContextDto?>;