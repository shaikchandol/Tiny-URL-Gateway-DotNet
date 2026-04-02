using MediatR;
using Microsoft.AspNetCore.Mvc;
using TinyUrl.Application.Queries.GetStatsSummary;
using TinyUrl.Application.Queries.GetTopUrls;

namespace TinyUrl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StatsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StatsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Get global statistics summary</summary>
    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Summary(CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetStatsSummaryQuery(), ct);
        return Ok(result);
    }

    /// <summary>Get top URLs by click count</summary>
    [HttpGet("top-urls")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TopUrls([FromQuery] int limit = 10, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetTopUrlsQuery(limit), ct);
        return Ok(result);
    }
}
