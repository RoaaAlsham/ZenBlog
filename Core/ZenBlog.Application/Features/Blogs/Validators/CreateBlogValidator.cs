using FluentValidation;
using FluentValidation.Validators;
using System;
using System.Collections.Generic;
using System.Text;
using ZenBlog.Application.Features.Blogs.Commands;

namespace ZenBlog.Application.Features.Blogs.Validators
{
    public class CreateBlogValidator: AbstractValidator<CreateBlogCommand>
    {
        public CreateBlogValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.");
            RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required.");
            // UserId is taken from the JWT in CreateBlogCommandHandler — do not require it from the client.
            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Category is required.");
        }
    }
}
