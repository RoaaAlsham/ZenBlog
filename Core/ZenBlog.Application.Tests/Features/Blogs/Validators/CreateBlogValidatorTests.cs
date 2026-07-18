using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;
using ZenBlog.Application.Features.Blogs.Commands;
using ZenBlog.Application.Features.Blogs.Validators;
using ZenBlog.Application.Models;

namespace ZenBlog.Application.Tests.Features.Blogs.Validators;

public class CreateBlogValidatorTests
{
    private readonly CreateBlogValidator _validator = new(
        Options.Create(new CloudinarySettings { CloudName = "demo" }));

    [Fact]
    public void TitleRule_FailsWhenEmpty_PassesWhenProvided()
    {
        var invalid = BuildValidCommand();
        invalid.Title = string.Empty;
        var invalidResult = _validator.TestValidate(invalid);
        invalidResult.ShouldHaveValidationErrorFor(x => x.Title);

        var valid = BuildValidCommand();
        valid.Title = "Valid title";
        var validResult = _validator.TestValidate(valid);
        validResult.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void DescriptionRule_FailsWhenEmpty_PassesWhenProvided()
    {
        var invalid = BuildValidCommand();
        invalid.Description = string.Empty;
        var invalidResult = _validator.TestValidate(invalid);
        invalidResult.ShouldHaveValidationErrorFor(x => x.Description);

        var valid = BuildValidCommand();
        valid.Description = "Valid description";
        var validResult = _validator.TestValidate(valid);
        validResult.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void CategoryIdRule_FailsWhenEmpty_PassesWhenProvided()
    {
        var invalid = BuildValidCommand();
        invalid.CategoryId = Guid.Empty;
        var invalidResult = _validator.TestValidate(invalid);
        invalidResult.ShouldHaveValidationErrorFor(x => x.CategoryId);

        var valid = BuildValidCommand();
        valid.CategoryId = Guid.NewGuid();
        var validResult = _validator.TestValidate(valid);
        validResult.ShouldNotHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void UserId_IsNotRequired_BecauseOwnershipComesFromJwt()
    {
        var command = BuildValidCommand();
        command.UserId = string.Empty;
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void CoverImage_RejectsNonCloudinaryUrl()
    {
        var command = BuildValidCommand();
        command.CoverImageUrl = "https://cdn.example.com/cover.png";
        command.CoverImagePublicId = "cover";
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CoverImageUrl);
    }

    [Fact]
    public void CoverImage_AcceptsCloudinaryPair()
    {
        var command = BuildValidCommand();
        command.CoverImageUrl = "https://res.cloudinary.com/demo/image/upload/v1/zenblog/covers/c.png";
        command.CoverImagePublicId = "zenblog/covers/c";
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    private static CreateBlogCommand BuildValidCommand()
        => new()
        {
            Title = "My blog",
            Description = "My description",
            CoverImageUrl = null,
            CoverImagePublicId = null,
            CategoryId = Guid.NewGuid(),
            UserId = null!
        };
}
