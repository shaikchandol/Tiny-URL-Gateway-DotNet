using Microsoft.EntityFrameworkCore;
using TinyUrl.Application.Interfaces;
using TinyUrl.Domain.Entities;
using TinyUrl.Infrastructure.Data;

namespace TinyUrl.Infrastructure.Repositories;

public class UrlRepository : IUrlRepository
{
    private readonly AppDbContext _db;

    public UrlRepository(AppDbContext db) => _db = db;

    public async Task<ShortUrl?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Urls.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken ct = default) =>
        await _db.Urls.FirstOrDefaultAsync(u => u.ShortCode == shortCode, ct);

    public async Task<(IEnumerable<ShortUrl> Urls, int Total)> ListAsync(int page, int limit, string? search, CancellationToken ct = default)
    {
        var query = _db.Urls.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.LongUrl.Contains(search) || u.ShortCode.Contains(search));

        var total = await query.CountAsync(ct);
        var urls = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(ct);

        return (urls, total);
    }

    public async Task<IEnumerable<ShortUrl>> GetTopByClicksAsync(int limit, CancellationToken ct = default) =>
        await _db.Urls
            .OrderByDescending(u => u.ClickCount)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<ShortUrl> AddAsync(ShortUrl url, CancellationToken ct = default)
    {
        _db.Urls.Add(url);
        await _db.SaveChangesAsync(ct);
        return url;
    }

    public async Task UpdateAsync(ShortUrl url, CancellationToken ct = default)
    {
        _db.Urls.Update(url);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken ct = default) =>
        await _db.Urls.IgnoreQueryFilters().AnyAsync(u => u.ShortCode == shortCode, ct);

    public async Task<int> GetTotalUrlsAsync(CancellationToken ct = default) =>
        await _db.Urls.CountAsync(ct);

    public async Task<int> GetTotalClicksAsync(CancellationToken ct = default) =>
        await _db.Urls.SumAsync(u => u.ClickCount, ct);

    public async Task<int> GetUrlsCreatedTodayAsync(CancellationToken ct = default)
    {
        var today = DateTimeOffset.UtcNow.Date;
        return await _db.Urls.CountAsync(u => u.CreatedAt >= today, ct);
    }

    public async Task<int> GetClicksTodayAsync(CancellationToken ct = default)
    {
        var today = DateTimeOffset.UtcNow.Date;
        return await _db.ClickEvents.CountAsync(e => e.ClickedAt >= today, ct);
    }

    public async Task AddClickEventAsync(ClickEvent clickEvent, CancellationToken ct = default)
    {
        _db.ClickEvents.Add(clickEvent);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<(string Date, int Clicks)>> GetClicksByDateAsync(Guid urlId, int days, CancellationToken ct = default)
    {
        var from = DateTimeOffset.UtcNow.AddDays(-days);
        var results = await _db.ClickEvents
            .Where(e => e.UrlId == urlId && e.ClickedAt >= from)
            .GroupBy(e => e.ClickedAt.Date)
            .Select(g => new { Date = g.Key.ToString("yyyy-MM-dd"), Clicks = g.Count() })
            .OrderBy(g => g.Date)
            .ToListAsync(ct);

        return results.Select(r => (r.Date, r.Clicks));
    }
}
