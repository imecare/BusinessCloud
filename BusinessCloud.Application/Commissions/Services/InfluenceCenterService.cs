using BusinessCloud.Application.Commissions.Dtos;

using BusinessCloud.Application.Commissions.Interfaces;
using BusinessCloud.Domain.Commissions.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


//namespace BusinessCloud.Application.Commissions.Services
//{
//    public class InfluenceCenterService : IInfluenceCenterService
//    {
//        private readonly ICOMISS _db;
//        private readonly PasswordHasher<InfluenceCenter> _hasher = new();

//        public InfluenceCenterService(ICommissionsDbContext db)
//        {
//            _db = db;
//        }

//        public async Task<InfluenceCenterResponse> CreateAsync(InfluenceCenterCreateRequest req)
//        {
//            req.RFC = req.RFC?.Trim().ToUpperInvariant() ?? "";
//            req.Email = req.Email?.Trim() ?? "";
//            req.Username = req.Username?.Trim();

//            if (string.IsNullOrWhiteSpace(req.Name))
//                throw new ArgumentException("Name es requerido.");

//            if (string.IsNullOrWhiteSpace(req.RFC))
//                throw new ArgumentException("RFC es requerido.");

//            if (string.IsNullOrWhiteSpace(req.Email))
//                throw new ArgumentException("Email es requerido.");

//            var existsRfc = await _db.InfluenceCenters.AnyAsync(x => x.RFC == req.RFC);
//            if (existsRfc)
//                throw new InvalidOperationException("Ya existe un Centro de Influencia con ese RFC.");

//            if (!string.IsNullOrWhiteSpace(req.Username))
//            {
//                var existsUser = await _db.InfluenceCenters.AnyAsync(x => x.Username == req.Username);
//                if (existsUser)
//                    throw new InvalidOperationException("Ese Username ya está en uso.");
//            }

//            var entity = new InfluenceCenter
//            {
//                Name = req.Name.Trim(),
//                RFC = req.RFC,
//                Email = req.Email,
//                Username = string.IsNullOrWhiteSpace(req.Username) ? null : req.Username,
//                IsActive = true,
//                Role = "InfluenceCenter"
//            };

//            if (!string.IsNullOrWhiteSpace(req.Password))
//            {
//                entity.PasswordHash = _hasher.HashPassword(entity, req.Password);
//            }

//            _db.InfluenceCenters.Add(entity);
//            await _db.SaveChangesAsync();

//            return Map(entity);
//        }

//        public async Task<List<InfluenceCenterResponse>> GetAllAsync(bool includeInactive = false)
//        {
//            var query = _db.InfluenceCenters.AsNoTracking();

//            if (!includeInactive)
//                query = query.Where(x => x.IsActive);

//            var list = await query.OrderBy(x => x.Name).ToListAsync();

//            return list.Select(Map).ToList();
//        }

//        public async Task<InfluenceCenterResponse?> GetByIdAsync(int id)
//        {
//            var entity = await _db.InfluenceCenters
//                .AsNoTracking()
//                .FirstOrDefaultAsync(x => x.Id == id);

//            return entity == null ? null : Map(entity);
//        }

//        public async Task<InfluenceCenterResponse> UpdateAsync(int id, InfluenceCenterUpdateRequest req)
//        {
//            var entity = await _db.InfluenceCenters.FirstOrDefaultAsync(x => x.Id == id);

//            if (entity == null)
//                throw new KeyNotFoundException("Centro de Influencia no encontrado.");

//            if (string.IsNullOrWhiteSpace(req.Name))
//                throw new ArgumentException("Name es requerido.");

//            if (string.IsNullOrWhiteSpace(req.Email))
//                throw new ArgumentException("Email es requerido.");

//            entity.Name = req.Name.Trim();
//            entity.Email = req.Email.Trim();

//            await _db.SaveChangesAsync();

//            return Map(entity);
//        }

//        public async Task DeactivateAsync(int id)
//        {
//            var entity = await _db.InfluenceCenters.FirstOrDefaultAsync(x => x.Id == id);

//            if (entity == null)
//                throw new KeyNotFoundException("Centro de Influencia no encontrado.");

//            entity.IsActive = false;

//            await _db.SaveChangesAsync();
//        }

//        public async Task ActivateAsync(int id)
//        {
//            var entity = await _db.InfluenceCenters.FirstOrDefaultAsync(x => x.Id == id);

//            if (entity == null)
//                throw new KeyNotFoundException("Centro de Influencia no encontrado.");

//            entity.IsActive = true;

//            await _db.SaveChangesAsync();
//        }

//        public async Task SetCredentialsAsync(int id, InfluenceCenterSetCredentialsRequest req)
//        {
//            var entity = await _db.InfluenceCenters.FirstOrDefaultAsync(x => x.Id == id);

//            if (entity == null)
//                throw new KeyNotFoundException("Centro de Influencia no encontrado.");

//            req.Username = req.Username?.Trim() ?? "";

//            if (string.IsNullOrWhiteSpace(req.Username))
//                throw new ArgumentException("Username es requerido.");

//            if (string.IsNullOrWhiteSpace(req.Password))
//                throw new ArgumentException("Password es requerido.");

//            var existsUser = await _db.InfluenceCenters
//                .AnyAsync(x => x.Username == req.Username && x.Id != id);

//            if (existsUser)
//                throw new InvalidOperationException("Ese Username ya está en uso.");

//            entity.Username = req.Username;
//            entity.PasswordHash = _hasher.HashPassword(entity, req.Password);

//            await _db.SaveChangesAsync();
//        }

//        private static InfluenceCenterResponse Map(InfluenceCenter x) => new()
//        {
//            Id = x.Id,
//            Name = x.Name,
//            RFC = x.RFC,
//            Email = x.Email,
//            Username = x.Username,
//            IsActive = x.IsActive,
//            CreatedAt = x.CreatedAt
//        };
//    }
//}