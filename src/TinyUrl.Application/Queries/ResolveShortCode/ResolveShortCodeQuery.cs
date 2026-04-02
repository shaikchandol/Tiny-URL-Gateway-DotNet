using MediatR;
using TinyUrl.Application.DTOs;

namespace TinyUrl.Application.Queries.ResolveShortCode;

public record ResolveShortCodeQuery(string ShortCode) : IRequest<ResolveResponseDto>;
