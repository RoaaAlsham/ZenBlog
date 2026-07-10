using FluentValidation.TestHelper;
using ZenBlog.Application.Features.Blogs.Commands;
using ZenBlog.Application.Features.Blogs.Validators;

namespace ZenBlog.Application.Tests.Features.Blogs.Validators;

public class CreateBlogValidatorTests
{
    private readonly CreateBlogValidator _validator = new();

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

    private static CreateBlogCommand BuildValidCommand()
        => new()
        {
            Title = "My blog",
            Description = "My description",
            CoverImageUrl = "cover.png",
            BlogImageUrl = "blog.png",
            CategoryId = Guid.NewGuid(),
            UserId = null!
        };
}
