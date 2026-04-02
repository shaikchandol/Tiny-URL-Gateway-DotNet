using MediatR;
using Microsoft.AspNetCore.Mvc;
using TinyUrl.Application.Commands.CreateShortUrl;
using TinyUrl.Application.Commands.DeleteUrl;
using TinyUrl.Application.Commands.UpdateUrl;
using TinyUrl.Application.Queries.GetUrl;
using TinyUrl.Application.Queries.GetUrlClicks;
using TinyUrl.Application.Queries.ListUrls;

namespace TinyUrl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UrlsController : ControllerBase
{
    private readonly IMediator _mediator;

    public UrlsController(IMediator mediator) => _mediator = mediator;

    /// <summary>List all shortened URLs with optional search and pagination</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListUrlsQuery(page, limit, search), ct);
        return Ok(result);
    }

    /// <summary>Get a specific URL by ID</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetUrlQuery(id), ct);
        return Ok(result);
    }

    /// <summary>Create a new shortened URL</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateShortUrlCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Update an existing URL</summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUrlRequest body, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new UpdateUrlCommand(id, body.LongUrl, body.ExpiresAt), ct);
        return Ok(result);
    }

    /// <summary>Soft-delete a URL</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        await _mediator.Send(new DeleteUrlCommand(id), ct);
        return NoContent();
    }

    /// <summary>Get click time-series for a URL</summary>
    [HttpGet("{id:guid}/clicks")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClicks(Guid id, [FromQuery] int days = 30, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetUrlClicksQuery(id, days), ct);
        return Ok(result);
    }
}

public record UpdateUrlRequest(string? LongUrl, string? ExpiresAt);
