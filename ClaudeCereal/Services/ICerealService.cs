using ClaudeCereal.Models;

namespace ClaudeCereal.Services;

public interface ICerealService
{
    Task<IEnumerable<Cereal>> GetAllAsync();
    Task<Cereal?> GetByIdAsync(int id);
    Task<Cereal> CreateAsync(Cereal cereal);
    Task<Cereal?> UpdateAsync(int id, Cereal input);
    Task<bool> DeleteAsync(int id);
}
