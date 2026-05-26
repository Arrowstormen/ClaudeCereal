using ClaudeCereal.Models;

namespace ClaudeCereal.Services;

public interface ICerealService
{
    Task<IEnumerable<Cereal>> GetAllAsync();
    Task<Cereal?> GetByIdAsync(int id);
    Task<Cereal> CreateAsync(CerealDto dto);
    Task<Cereal?> UpdateAsync(int id, CerealDto dto);
    Task<bool> DeleteAsync(int id);
}
