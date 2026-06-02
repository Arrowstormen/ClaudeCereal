using ClaudeCereal.Models;

namespace ClaudeCereal.Services;

public interface ICerealService
{
    Task<PagedResult<Cereal>> GetFilteredAsync(CerealFilter filter,          CancellationToken cancellationToken = default);
    Task<Cereal?>             GetByIdAsync    (int id,                        CancellationToken cancellationToken = default);
    Task<bool>                IsDeletedAsync  (int id,                        CancellationToken cancellationToken = default);
    Task<Cereal>              CreateAsync     (CerealRequest request,          CancellationToken cancellationToken = default);
    Task<Cereal?>             UpdateAsync     (int id, CerealRequest request,  CancellationToken cancellationToken = default);
    Task<bool>                DeleteAsync     (int id,                        CancellationToken cancellationToken = default);
    Task<Cereal?>             RestoreAsync    (int id,                        CancellationToken cancellationToken = default);
}
