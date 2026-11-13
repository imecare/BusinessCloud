using MediatR;

namespace BusinessCloud.Application.Admin.Commands.SetCompanyModules;

public sealed record SetCompanyModulesCommand(string TenantId, string[] Modules)
    : IRequest<IReadOnlyList<string>>;
