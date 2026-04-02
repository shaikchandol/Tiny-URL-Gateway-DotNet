using MediatR;
using TinyUrl.Application.DTOs;
using TinyUrl.Application.Interfaces;

namespace TinyUrl.Application.Queries.ListUrls;

public class ListUrlsHandler : IRequestHandler<ListUrlsQuery, UrlListResponseDto>
{
    private readonly IUrlRepository _repository;

    public ListUrlsHandler(IUrlRepository repository) => _repository = repository;

    public async Task<UrlListResponseDto> Handle(ListUrlsQuery request, CancellationToken cancellationToken)
    {
        var (urls, total) = await _repository.ListAsync(request.Page, request.Limit, request.Search, cancellationToken);

        var dtos = urls.Select(u => new ShortUrlDto(u.Id, u.ShortCode, u.LongUrl, u.CustomAlias, u.ClickCount,
            u.ExpiresAt?.ToString("O"), u.CreatedAt.ToString("O"), u.UpdatedAt.ToString("O")));

        return new UrlListResponseDto(dtos, total, request.Page, request.Limit);
    }
}
