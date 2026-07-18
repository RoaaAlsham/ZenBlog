using FluentValidation;
using Microsoft.Extensions.Options;
using ZenBlog.Application.Features.Blogs.Commands;
using ZenBlog.Application.Features.Media;
using ZenBlog.Application.Models;

namespace ZenBlog.Application.Features.Blogs.Validators
{
    public class CreateBlogValidator: AbstractValidator<CreateBlogCommand>
    {
        public CreateBlogValidator(IOptions<CloudinarySettings> cloudinaryOptions)
        {
            var cloudName = cloudinaryOptions.Value.CloudName;

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.");
            RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required.");
            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Category is required.");

            RuleFor(x => x)
                .Must(x => CloudinaryImageRules.BothSetOrBothMissing(x.CoverImageUrl, x.CoverImagePublicId))
                .WithMessage("CoverImageUrl and CoverImagePublicId must both be set or both be empty.");

            RuleFor(x => x.CoverImageUrl)
                .MaximumLength(2048)
                .Must(url => CloudinaryImageRules.IsCloudinaryDeliveryUrl(url, cloudName))
                .When(x => !string.IsNullOrWhiteSpace(x.CoverImageUrl))
                .WithMessage("CoverImageUrl must be a Cloudinary delivery URL for this cloud.");

            RuleFor(x => x.CoverImagePublicId)
                .MaximumLength(512)
                .When(x => !string.IsNullOrWhiteSpace(x.CoverImagePublicId));
        }
    }
}
