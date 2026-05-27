using ClaudeCereal.Models;

namespace ClaudeCereal.Services;

public interface ICerealService
{
    Task<IEnumerable<Cereal>> GetAllAsync();
    Task<Cereal?> GetByIdAsync(int id);
    Task<Cereal> CreateAsync(CerealRequest dto);
    Task<Cereal?> UpdateAsync(int id, CerealRequest dto);
    Task<bool> DeleteAsync(int id);
}
