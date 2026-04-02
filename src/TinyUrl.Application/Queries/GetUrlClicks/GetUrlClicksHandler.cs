using MediatR;
using TinyUrl.Application.DTOs;
using TinyUrl.Application.Interfaces;

namespace TinyUrl.Application.Queries.GetUrlClicks;

public class GetUrlClicksHandler : IRequestHandler<GetUrlClicksQuery, ClickTimeSeriesDto>
{
    private readonly IUrlRepository _repository;

    public GetUrlClicksHandler(IUrlRepository repository) => _repository = repository;

    public async Task<ClickTimeSeriesDto> Handle(GetUrlClicksQuery request, CancellationToken cancellationToken)
    {
        var url = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"URL with id '{request.Id}' not found.");

        var data = await _repository.GetClicksByDateAsync(request.Id, request.Days, cancellationToken);
        var points = data.Select(d => new ClickDataPointDto(d.Date, d.Clicks));

        return new ClickTimeSeriesDto(url.ShortCode, url.ClickCount, points);
    }
}
