namespace TinyUrl.Domain.Entities;

public class ClickEvent
{
    public Guid Id { get; private set; }
    public Guid UrlId { get; private set; }
    public string ShortCode { get; private set; } = string.Empty;
    public DateTimeOffset ClickedAt { get; private set; }

    public ShortUrl? ShortUrl { get; private set; }

    private ClickEvent() { }

    public static ClickEvent Create(Guid urlId, string shortCode)
    {
        return new ClickEvent
        {
            Id = Guid.NewGuid(),
            UrlId = urlId,
            ShortCode = shortCode,
            ClickedAt = DateTimeOffset.UtcNow
        };
    }
}
