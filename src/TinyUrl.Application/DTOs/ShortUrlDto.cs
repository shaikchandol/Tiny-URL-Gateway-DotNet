namespace TinyUrl.Application.DTOs;

public record ShortUrlDto(
    Guid Id,
    string ShortCode,
    string LongUrl,
    string? CustomAlias,
    int ClickCount,
    string? ExpiresAt,
    string CreatedAt,
    string UpdatedAt
);

public record UrlListResponseDto(
    IEnumerable<ShortUrlDto> Urls,
    int Total,
    int Page,
    int Limit
);

public record StatsSummaryDto(
    int TotalUrls,
    int TotalClicks,
    int UrlsCreatedToday,
    int ClicksToday,
    double AvgClicksPerUrl
);

public record ClickDataPointDto(string Date, int Clicks);

public record ClickTimeSeriesDto(
    string ShortCode,
    int TotalClicks,
    IEnumerable<ClickDataPointDto> Data
);

public record ResolveResponseDto(string LongUrl, string ShortCode);

public record ErrorResponseDto(string Error, string Message);
