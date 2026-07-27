using FluentValidation.TestHelper;
using ZenBlog.Application.Features.Comments.Commands;
using ZenBlog.Application.Features.Comments.Validators;

namespace ZenBlog.Application.Tests.Features.Comments.Validators;

public class CreateCommentCommandValidationTests
{
    private readonly CreateCommentCommandValidator _validator = new();

    [Fact]
    public void BodyNotEmptyRule_FailsWhenEmpty_PassesWhenProvided()
    {
        var invalid = BuildValidCommand();
        invalid.Body = string.Empty;
        var invalidResult = _validator.TestValidate(invalid);
        invalidResult.ShouldHaveValidationErrorFor(x => x.Body);

        var valid = BuildValidCommand();
        valid.Body = "Valid comment";
        var validResult = _validator.TestValidate(valid);
        validResult.ShouldNotHaveValidationErrorFor(x => x.Body);
    }

    [Fact]
    public void BodyMaxLengthRule_FailsWhenOver1000_PassesAt1000OrLess()
    {
        var invalid = BuildValidCommand();
        invalid.Body = new string('a', 1001);
        var invalidResult = _validator.TestValidate(invalid);
        invalidResult.ShouldHaveValidationErrorFor(x => x.Body);

        var valid = BuildValidCommand();
        valid.Body = new string('a', 1000);
        var validResult = _validator.TestValidate(valid);
        validResult.ShouldNotHaveValidationErrorFor(x => x.Body);
    }

    [Fact]
    public void BlogIdRule_FailsWhenEmptyGuid_PassesWhenNonEmpty()
    {
        var invalid = BuildValidCommand();
        invalid.BlogId = Guid.Empty;
        var invalidResult = _validator.TestValidate(invalid);
        invalidResult.ShouldHaveValidationErrorFor(x => x.BlogId);

        var valid = BuildValidCommand();
        valid.BlogId = Guid.NewGuid();
        var validResult = _validator.TestValidate(valid);
        validResult.ShouldNotHaveValidationErrorFor(x => x.BlogId);
    }

    private static CreateCommentCommand BuildValidCommand()
        => new()
        {
            Body = "Base comment",
            BlogId = Guid.NewGuid(),
            ParentCommentId = null
        };
}
