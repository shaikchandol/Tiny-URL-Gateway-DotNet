using MediatR;
using TinyUrl.Application.DTOs;
using TinyUrl.Application.Interfaces;

namespace TinyUrl.Application.Commands.UpdateUrl;

public class UpdateUrlHandler : IRequestHandler<UpdateUrlCommand, ShortUrlDto>
{
    private readonly IUrlRepository _repository;
    private readonly ICacheService _cache;

    public UpdateUrlHandler(IUrlRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<ShortUrlDto> Handle(UpdateUrlCommand request, CancellationToken cancellationToken)
    {
        var url = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"URL with id '{request.Id}' not found.");

        if (!string.IsNullOrWhiteSpace(request.LongUrl))
            url.UpdateLongUrl(request.LongUrl);

        if (request.ExpiresAt is not null)
        {
            DateTimeOffset? expiry = string.IsNullOrWhiteSpace(request.ExpiresAt)
                ? null
                : DateTimeOffset.Parse(request.ExpiresAt);
            url.UpdateExpiry(expiry);
        }

        await _repository.UpdateAsync(url, cancellationToken);
        await _cache.RemoveAsync($"url:{url.ShortCode}", cancellationToken);

        return new ShortUrlDto(url.Id, url.ShortCode, url.LongUrl, url.CustomAlias, url.ClickCount,
            url.ExpiresAt?.ToString("O"), url.CreatedAt.ToString("O"), url.UpdatedAt.ToString("O"));
    }
}
