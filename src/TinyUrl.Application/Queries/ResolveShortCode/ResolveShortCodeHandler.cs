using MediatR;
using TinyUrl.Application.DTOs;
using TinyUrl.Application.Interfaces;
using TinyUrl.Domain.Entities;

namespace TinyUrl.Application.Queries.ResolveShortCode;

public class ResolveShortCodeHandler : IRequestHandler<ResolveShortCodeQuery, ResolveResponseDto>
{
    private readonly IUrlRepository _repository;
    private readonly ICacheService _cache;

    public ResolveShortCodeHandler(IUrlRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<ResolveResponseDto> Handle(ResolveShortCodeQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"url:{request.ShortCode}";
        var cached = await _cache.GetAsync(cacheKey, cancellationToken);

        if (cached is not null)
            return new ResolveResponseDto(cached, request.ShortCode);

        var url = await _repository.GetByShortCodeAsync(request.ShortCode, cancellationToken)
            ?? throw new KeyNotFoundException($"Short code '{request.ShortCode}' not found.");

        if (url.IsExpired())
            throw new InvalidOperationException($"Short code '{request.ShortCode}' has expired.");

        url.IncrementClickCount();
        var clickEvent = ClickEvent.Create(url.Id, url.ShortCode);

        await _repository.UpdateAsync(url, cancellationToken);
        await _repository.AddClickEventAsync(clickEvent, cancellationToken);
        await _cache.SetAsync(cacheKey, url.LongUrl, TimeSpan.FromHours(24), cancellationToken);

        return new ResolveResponseDto(url.LongUrl, url.ShortCode);
    }
}
