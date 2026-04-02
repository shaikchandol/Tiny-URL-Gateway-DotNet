using MediatR;
using TinyUrl.Application.DTOs;

namespace TinyUrl.Application.Queries.GetTopUrls;

public record GetTopUrlsQuery(int Limit = 10) : IRequest<IEnumerable<ShortUrlDto>>;
