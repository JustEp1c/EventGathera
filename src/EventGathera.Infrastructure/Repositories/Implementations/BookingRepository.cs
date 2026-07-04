using EventGathera.Application.Repositories.Interfaces;
using EventGathera.Domain;
using EventGathera.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace EventGathera.Infrastructure.Repositories.Implementations;

/// <inheritdoc/>
public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _appDbContext;

    public BookingRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public IQueryable<Booking> GetAllBookingsQuery()
    {
        return _appDbContext.Bookings;
    }

    public async Task AddBookingAsync(Booking booking, CancellationToken ct = default)
    {
        await _appDbContext.AddAsync(booking, ct);
    }

    public async Task<Booking?> GetBookingByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _appDbContext.Bookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken: ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _appDbContext.SaveChangesAsync(ct);
    }
}
