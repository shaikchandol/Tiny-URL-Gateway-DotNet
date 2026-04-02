using FluentValidation;

namespace TinyUrl.Application.Commands.CreateShortUrl;

public class CreateShortUrlValidator : AbstractValidator<CreateShortUrlCommand>
{
    public CreateShortUrlValidator()
    {
        RuleFor(x => x.LongUrl)
            .NotEmpty().WithMessage("LongUrl is required.")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out var result)
                         && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps))
            .WithMessage("LongUrl must be a valid HTTP/HTTPS URL.");

        RuleFor(x => x.CustomAlias)
            .MaximumLength(50).WithMessage("Custom alias must be 50 characters or fewer.")
            .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Custom alias may only contain letters, numbers, hyphens, and underscores.")
            .When(x => !string.IsNullOrEmpty(x.CustomAlias));
    }
}
