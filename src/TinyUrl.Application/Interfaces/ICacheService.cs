namespace TinyUrl.Application.Interfaces;

public interface ICacheService
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task SetAsync(string key, string value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
