using CloudGames.Payments.Domain.Entities;
using CloudGames.Payments.Domain.Repositories;
using CloudGames.Payments.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CloudGames.Payments.Infra.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentsDbContext _db;

    public PaymentRepository(PaymentsDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Payment payment, CancellationToken ct = default)
    {
        await _db.Payments.AddAsync(payment, ct);
    }

    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Payments.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<IEnumerable<Payment>> GetPendingPaymentsAsync(CancellationToken ct = default)
    {
        return await _db.Payments
            .Where(p => p.Status == PaymentStatus.Pending)
            .ToListAsync(ct);
    }

    public Task UpdateAsync(Payment payment, CancellationToken ct = default)
    {
        _db.Payments.Update(payment);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
