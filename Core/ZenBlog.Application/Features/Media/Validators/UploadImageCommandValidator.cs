using FluentValidation;
using ZenBlog.Application.Features.Media.Commands;

namespace ZenBlog.Application.Features.Media.Validators;

public sealed class UploadImageCommandValidator : AbstractValidator<UploadImageCommand>
{
    public UploadImageCommandValidator()
    {
        RuleFor(x => x.Purpose)
            .IsInEnum()
            .WithMessage("Purpose must be Profile, BlogCover, or BlogBody.");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(260);

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ct => ImageUploadLimits.AllowedContentTypes.Contains(ct))
            .WithMessage("Only JPEG, PNG, WebP, and GIF images are allowed.");

        RuleFor(x => x.Length)
            .GreaterThan(0)
            .WithMessage("File is empty.")
            .LessThanOrEqualTo(ImageUploadLimits.MaxBytes)
            .WithMessage($"Image must be {ImageUploadLimits.MaxBytes / (1024 * 1024)} MB or smaller.");

        RuleFor(x => x.Content)
            .NotNull()
            .WithMessage("File content is required.");
    }
}
