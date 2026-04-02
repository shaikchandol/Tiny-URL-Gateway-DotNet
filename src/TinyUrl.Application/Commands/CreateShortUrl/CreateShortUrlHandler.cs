using MediatR;
using TinyUrl.Application.DTOs;
using TinyUrl.Application.Interfaces;
using TinyUrl.Domain.Entities;

namespace TinyUrl.Application.Commands.CreateShortUrl;

public class CreateShortUrlHandler : IRequestHandler<CreateShortUrlCommand, ShortUrlDto>
{
    private readonly IUrlRepository _repository;
    private readonly IShortCodeGenerator _codeGenerator;
    private readonly ICacheService _cache;

    public CreateShortUrlHandler(IUrlRepository repository, IShortCodeGenerator codeGenerator, ICacheService cache)
    {
        _repository = repository;
        _codeGenerator = codeGenerator;
        _cache = cache;
    }

    public async Task<ShortUrlDto> Handle(CreateShortUrlCommand request, CancellationToken cancellationToken)
    {
        string shortCode;

        if (!string.IsNullOrWhiteSpace(request.CustomAlias))
        {
            var exists = await _repository.ShortCodeExistsAsync(request.CustomAlias, cancellationToken);
            if (exists)
                throw new InvalidOperationException($"Custom alias '{request.CustomAlias}' is already taken.");
            shortCode = request.CustomAlias;
        }
        else
        {
            shortCode = await GenerateUniqueCodeAsync(cancellationToken);
        }

        DateTimeOffset? expiresAt = null;
        if (!string.IsNullOrWhiteSpace(request.ExpiresAt) && DateTimeOffset.TryParse(request.ExpiresAt, out var parsedExpiry))
            expiresAt = parsedExpiry;

        var url = ShortUrl.Create(shortCode, request.LongUrl, request.CustomAlias, expiresAt);
        var saved = await _repository.AddAsync(url, cancellationToken);

        await _cache.SetAsync(
            $"url:{shortCode}",
            saved.LongUrl,
            expiresAt.HasValue ? expiresAt.Value - DateTimeOffset.UtcNow : TimeSpan.FromHours(24),
            cancellationToken);

        return ToDto(saved);
    }

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        for (int i = 0; i < 5; i++)
        {
            var code = _codeGenerator.Generate();
            if (!await _repository.ShortCodeExistsAsync(code, cancellationToken))
                return code;
        }
        throw new InvalidOperationException("Failed to generate a unique short code after 5 attempts.");
    }

    private static ShortUrlDto ToDto(ShortUrl url) => new(
        url.Id,
        url.ShortCode,
        url.LongUrl,
        url.CustomAlias,
        url.ClickCount,
        url.ExpiresAt?.ToString("O"),
        url.CreatedAt.ToString("O"),
        url.UpdatedAt.ToString("O")
    );
}
