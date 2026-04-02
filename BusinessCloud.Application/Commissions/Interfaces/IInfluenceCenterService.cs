using BusinessCloud.Application.Commissions.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessCloud.Application.Commissions.Interfaces
{
    public interface IInfluenceCenterService
    {
        Task<InfluenceCenterResponse> CreateAsync(InfluenceCenterCreateRequest req);
        Task<List<InfluenceCenterResponse>> GetAllAsync(bool includeInactive = false);
        Task<InfluenceCenterResponse?> GetByIdAsync(int id);

        Task<InfluenceCenterResponse> UpdateAsync(int id, InfluenceCenterUpdateRequest req);
        Task DeactivateAsync(int id);
        Task ActivateAsync(int id);

        Task SetCredentialsAsync(int id, InfluenceCenterSetCredentialsRequest req);
    }
}
