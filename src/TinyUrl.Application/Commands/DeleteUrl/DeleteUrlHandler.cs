using MediatR;
using TinyUrl.Application.Interfaces;

namespace TinyUrl.Application.Commands.DeleteUrl;

public class DeleteUrlHandler : IRequestHandler<DeleteUrlCommand>
{
    private readonly IUrlRepository _repository;
    private readonly ICacheService _cache;

    public DeleteUrlHandler(IUrlRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task Handle(DeleteUrlCommand request, CancellationToken cancellationToken)
    {
        var url = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"URL with id '{request.Id}' not found.");

        url.SoftDelete();
        await _repository.UpdateAsync(url, cancellationToken);
        await _cache.RemoveAsync($"url:{url.ShortCode}", cancellationToken);
    }
}
