using MediatR;
using TinyUrl.Application.DTOs;

namespace TinyUrl.Application.Commands.CreateShortUrl;

public record CreateShortUrlCommand(
    string LongUrl,
    string? CustomAlias,
    string? ExpiresAt
) : IRequest<ShortUrlDto>;
