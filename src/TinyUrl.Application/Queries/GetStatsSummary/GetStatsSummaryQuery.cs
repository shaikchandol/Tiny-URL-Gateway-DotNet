using MediatR;
using TinyUrl.Application.DTOs;

namespace TinyUrl.Application.Queries.GetStatsSummary;

public record GetStatsSummaryQuery : IRequest<StatsSummaryDto>;
