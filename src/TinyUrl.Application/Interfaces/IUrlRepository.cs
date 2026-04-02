using TinyUrl.Domain.Entities;

namespace TinyUrl.Application.Interfaces;

public interface IUrlRepository
{
    Task<ShortUrl?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);
    Task<(IEnumerable<ShortUrl> Urls, int Total)> ListAsync(int page, int limit, string? search, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShortUrl>> GetTopByClicksAsync(int limit, CancellationToken cancellationToken = default);
    Task<ShortUrl> AddAsync(ShortUrl url, CancellationToken cancellationToken = default);
    Task UpdateAsync(ShortUrl url, CancellationToken cancellationToken = default);
    Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken = default);
    Task<int> GetTotalUrlsAsync(CancellationToken cancellationToken = default);
    Task<int> GetTotalClicksAsync(CancellationToken cancellationToken = default);
    Task<int> GetUrlsCreatedTodayAsync(CancellationToken cancellationToken = default);
    Task<int> GetClicksTodayAsync(CancellationToken cancellationToken = default);
    Task AddClickEventAsync(ClickEvent clickEvent, CancellationToken cancellationToken = default);
    Task<IEnumerable<(string Date, int Clicks)>> GetClicksByDateAsync(Guid urlId, int days, CancellationToken cancellationToken = default);
}
