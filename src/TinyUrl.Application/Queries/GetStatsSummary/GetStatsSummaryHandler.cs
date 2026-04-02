using MediatR;
using TinyUrl.Application.DTOs;
using TinyUrl.Application.Interfaces;

namespace TinyUrl.Application.Queries.GetStatsSummary;

public class GetStatsSummaryHandler : IRequestHandler<GetStatsSummaryQuery, StatsSummaryDto>
{
    private readonly IUrlRepository _repository;

    public GetStatsSummaryHandler(IUrlRepository repository) => _repository = repository;

    public async Task<StatsSummaryDto> Handle(GetStatsSummaryQuery request, CancellationToken cancellationToken)
    {
        var totalUrls = await _repository.GetTotalUrlsAsync(cancellationToken);
        var totalClicks = await _repository.GetTotalClicksAsync(cancellationToken);
        var urlsToday = await _repository.GetUrlsCreatedTodayAsync(cancellationToken);
        var clicksToday = await _repository.GetClicksTodayAsync(cancellationToken);
        var avg = totalUrls > 0 ? Math.Round((double)totalClicks / totalUrls, 2) : 0;

        return new StatsSummaryDto(totalUrls, totalClicks, urlsToday, clicksToday, avg);
    }
}
