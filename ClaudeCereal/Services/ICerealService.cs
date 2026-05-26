using ClaudeCereal.Models;

namespace ClaudeCereal.Services;

public interface ICerealService
{
    Task<IEnumerable<Cereal>> GetAllAsync(int page = 1, int pageSize = 50);
    Task<Cereal?> GetByIdAsync(int id);
    Task<Cereal> CreateAsync(Cereal cereal);
    Task<Cereal?> UpdateAsync(int id, Cereal input);
    Task<bool> DeleteAsync(int id);
}
