using MediatR;
using TinyUrl.Application.DTOs;

namespace TinyUrl.Application.Queries.GetUrl;

public record GetUrlQuery(Guid Id) : IRequest<ShortUrlDto>;
