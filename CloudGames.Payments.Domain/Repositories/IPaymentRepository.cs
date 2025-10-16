using CloudGames.Payments.Domain.Entities;

namespace CloudGames.Payments.Domain.Repositories;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken ct = default);
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Payment>> GetPendingPaymentsAsync(CancellationToken ct = default);
    Task UpdateAsync(Payment payment, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
