using MediatR;
using TinyUrl.Application.DTOs;
using TinyUrl.Application.Interfaces;

namespace TinyUrl.Application.Queries.GetTopUrls;

public class GetTopUrlsHandler : IRequestHandler<GetTopUrlsQuery, IEnumerable<ShortUrlDto>>
{
    private readonly IUrlRepository _repository;

    public GetTopUrlsHandler(IUrlRepository repository) => _repository = repository;

    public async Task<IEnumerable<ShortUrlDto>> Handle(GetTopUrlsQuery request, CancellationToken cancellationToken)
    {
        var urls = await _repository.GetTopByClicksAsync(request.Limit, cancellationToken);
        return urls.Select(u => new ShortUrlDto(u.Id, u.ShortCode, u.LongUrl, u.CustomAlias, u.ClickCount,
            u.ExpiresAt?.ToString("O"), u.CreatedAt.ToString("O"), u.UpdatedAt.ToString("O")));
    }
}
