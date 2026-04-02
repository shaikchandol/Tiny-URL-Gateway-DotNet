using MediatR;
using TinyUrl.Application.DTOs;

namespace TinyUrl.Application.Queries.GetUrlClicks;

public record GetUrlClicksQuery(Guid Id, int Days = 30) : IRequest<ClickTimeSeriesDto>;
