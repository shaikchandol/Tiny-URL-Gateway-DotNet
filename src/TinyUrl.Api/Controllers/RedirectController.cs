using MediatR;
using Microsoft.AspNetCore.Mvc;
using TinyUrl.Application.Queries.ResolveShortCode;

namespace TinyUrl.Api.Controllers;

[ApiController]
public class RedirectController : ControllerBase
{
    private readonly IMediator _mediator;

    public RedirectController(IMediator mediator) => _mediator = mediator;

    /// <summary>Resolve a short code and redirect to the original URL</summary>
    [HttpGet("/{shortCode}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> Redirect(string shortCode, CancellationToken ct = default)
    {
        if (shortCode.StartsWith("api") || shortCode.StartsWith("swagger") || shortCode.StartsWith("health"))
            return NotFound();

        var result = await _mediator.Send(new ResolveShortCodeQuery(shortCode), ct);
        return RedirectPermanent(result.LongUrl);
    }
}
