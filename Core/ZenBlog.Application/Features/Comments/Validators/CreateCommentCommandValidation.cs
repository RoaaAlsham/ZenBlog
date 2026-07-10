// Validators/CreateCommentCommandValidator.cs
using FluentValidation;
using ZenBlog.Application.Features.Comments.Commands;

namespace ZenBlog.Application.Features.Comments.Validators
{
    public class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>
    {
        public CreateCommentCommandValidator()
        {
            RuleFor(x => x.Body)
                .NotEmpty().WithMessage("Comment body is required.")
                .MaximumLength(1000).WithMessage("Comment cannot exceed 1000 characters.");

            RuleFor(x => x.BlogId)
                .NotEmpty().WithMessage("BlogId is required.");

            // UserId is taken from the JWT in CreateCommentCommandHandler — do not require it from the client.
        }
    }
}