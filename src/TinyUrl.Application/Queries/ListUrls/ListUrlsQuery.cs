using MediatR;
using TinyUrl.Application.DTOs;

namespace TinyUrl.Application.Queries.ListUrls;

public record ListUrlsQuery(int Page = 1, int Limit = 20, string? Search = null) : IRequest<UrlListResponseDto>;
