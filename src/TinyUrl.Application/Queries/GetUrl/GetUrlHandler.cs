using MediatR;
using TinyUrl.Application.DTOs;
using TinyUrl.Application.Interfaces;

namespace TinyUrl.Application.Queries.GetUrl;

public class GetUrlHandler : IRequestHandler<GetUrlQuery, ShortUrlDto>
{
    private readonly IUrlRepository _repository;

    public GetUrlHandler(IUrlRepository repository) => _repository = repository;

    public async Task<ShortUrlDto> Handle(GetUrlQuery request, CancellationToken cancellationToken)
    {
        var url = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"URL with id '{request.Id}' not found.");

        return new ShortUrlDto(url.Id, url.ShortCode, url.LongUrl, url.CustomAlias, url.ClickCount,
            url.ExpiresAt?.ToString("O"), url.CreatedAt.ToString("O"), url.UpdatedAt.ToString("O"));
    }
}
