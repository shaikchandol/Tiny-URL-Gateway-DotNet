using MediatR;

namespace TinyUrl.Application.Commands.DeleteUrl;

public record DeleteUrlCommand(Guid Id) : IRequest;
