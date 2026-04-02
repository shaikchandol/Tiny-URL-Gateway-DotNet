using MediatR;
using TinyUrl.Application.DTOs;

namespace TinyUrl.Application.Commands.UpdateUrl;

public record UpdateUrlCommand(Guid Id, string? LongUrl, string? ExpiresAt) : IRequest<ShortUrlDto>;
