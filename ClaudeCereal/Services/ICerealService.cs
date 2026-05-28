using ClaudeCereal.Models;

namespace ClaudeCereal.Services;

public interface ICerealService
{
    Task<PagedResult<Cereal>> GetFilteredAsync(CerealFilter filter);
    Task<Cereal?> GetByIdAsync(int id);
    Task<Cereal> CreateAsync(CerealRequest request);
    Task<Cereal?> UpdateAsync(int id, CerealRequest request);
    Task<bool> DeleteAsync(int id);
    Task<Cereal?> RestoreAsync(int id);
}
