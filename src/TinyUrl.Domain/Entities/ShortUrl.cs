namespace TinyUrl.Domain.Entities;

public class ShortUrl
{
    public Guid Id { get; private set; }
    public string ShortCode { get; private set; } = string.Empty;
    public string LongUrl { get; private set; } = string.Empty;
    public string? CustomAlias { get; private set; }
    public int ClickCount { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public ICollection<ClickEvent> ClickEvents { get; private set; } = new List<ClickEvent>();

    private ShortUrl() { }

    public static ShortUrl Create(string shortCode, string longUrl, string? customAlias = null, DateTimeOffset? expiresAt = null)
    {
        return new ShortUrl
        {
            Id = Guid.NewGuid(),
            ShortCode = shortCode,
            LongUrl = longUrl,
            CustomAlias = customAlias,
            ClickCount = 0,
            ExpiresAt = expiresAt,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void IncrementClickCount()
    {
        ClickCount++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateLongUrl(string newLongUrl)
    {
        LongUrl = newLongUrl;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateExpiry(DateTimeOffset? newExpiry)
    {
        ExpiresAt = newExpiry;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsExpired() => ExpiresAt.HasValue && ExpiresAt.Value < DateTimeOffset.UtcNow;
}
