using FluentValidation.TestHelper;
using ZenBlog.Application.Features.Categories.Commands;
using ZenBlog.Application.Features.Categories.Validators;

namespace ZenBlog.Application.Tests.Features.Categories.Validators;

public class CreateCategoryValidatorTests
{
    private readonly CreateCategoryValidator _validator = new();

    [Fact]
    public void CategoryNameNotEmptyRule_FailsWhenEmpty_PassesWhenProvided()
    {
        var invalid = new CreateCategoryCommand { CategoryName = string.Empty };
        var invalidResult = _validator.TestValidate(invalid);
        invalidResult.ShouldHaveValidationErrorFor(x => x.CategoryName);

        var valid = new CreateCategoryCommand { CategoryName = "Tech" };
        var validResult = _validator.TestValidate(valid);
        validResult.ShouldNotHaveValidationErrorFor(x => x.CategoryName);
    }

    [Fact]
    public void CategoryNameNotNullRule_FailsWhenNull_PassesWhenProvided()
    {
        var invalid = new CreateCategoryCommand { CategoryName = null! };
        var invalidResult = _validator.TestValidate(invalid);
        invalidResult.ShouldHaveValidationErrorFor(x => x.CategoryName)
            .WithErrorMessage("Name cannot be null");

        var valid = new CreateCategoryCommand { CategoryName = "Travel" };
        var validResult = _validator.TestValidate(valid);
        validResult.ShouldNotHaveValidationErrorFor(x => x.CategoryName);
    }

    [Fact]
    public void CategoryNameMaxLengthRule_FailsWhenOver100_PassesAt100OrLess()
    {
        var invalid = new CreateCategoryCommand { CategoryName = new string('a', 101) };
        var invalidResult = _validator.TestValidate(invalid);
        invalidResult.ShouldHaveValidationErrorFor(x => x.CategoryName)
            .WithErrorMessage("Name cannot exceed 100 characters");

        var valid = new CreateCategoryCommand { CategoryName = new string('a', 100) };
        var validResult = _validator.TestValidate(valid);
        validResult.ShouldNotHaveValidationErrorFor(x => x.CategoryName);
    }
}
