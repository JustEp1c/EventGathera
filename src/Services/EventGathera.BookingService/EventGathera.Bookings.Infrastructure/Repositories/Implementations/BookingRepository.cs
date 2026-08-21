using EventGathera.Bookings.Application.Repositories.Interfaces;
using EventGathera.Bookings.Domain.Enums;
using EventGathera.Bookings.Entities.Domain;
using EventGathera.Bookings.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace EventGathera.Bookings.Infrastructure.Repositories.Implementations;

/// <inheritdoc/>
public class BookingRepository : IBookingRepository
{
    private readonly BookingsDbContext _appDbContext;

    public BookingRepository(BookingsDbContext appDbContext)
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
        return await _appDbContext.Bookings
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken: ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _appDbContext.SaveChangesAsync(ct);
    }

    public async Task<int> GetActiveBookingsCountByUserAsync(Guid userId, CancellationToken ct)
    {
        return await _appDbContext.Bookings
            .CountAsync(b => b.UserId == userId && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed), ct);
    }
}
