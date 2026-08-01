using FluentValidation;
using Microsoft.Extensions.Options;
using ZenBlog.Application.Features.Media;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Application.Models;

namespace ZenBlog.Application.Features.Users.Validators;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator(IOptions<CloudinarySettings> cloudinaryOptions)
    {
        var cloudName = cloudinaryOptions.Value.CloudName;

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x)
            .Must(x => CloudinaryImageRules.BothSetOrBothMissing(x.ImageUrl, x.ImagePublicId))
            .WithMessage("ImageUrl and ImagePublicId must both be set or both be empty.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(2048)
            .Must(url => CloudinaryImageRules.IsCloudinaryDeliveryUrl(url, cloudName))
            .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl))
            .WithMessage("ImageUrl must be a Cloudinary delivery URL for this cloud.");

        RuleFor(x => x.ImagePublicId)
            .MaximumLength(512)
            .Must((command, publicId) =>
                CloudinaryImageRules.PublicIdMatchesDeliveryUrl(
                    command.ImageUrl, publicId, cloudName))
            .When(x => !string.IsNullOrWhiteSpace(x.ImagePublicId))
            .WithMessage("ImagePublicId must match the ImageUrl delivery path.")
            .Must(publicId => CloudinaryImageRules.HasFolderPrefix(
                publicId, ImageUploadLimits.FolderFor(ImageUploadPurpose.Profile)))
            .When(x => !string.IsNullOrWhiteSpace(x.ImagePublicId))
            .WithMessage("ImagePublicId must be under the zenblog/profiles folder.");
    }
}
