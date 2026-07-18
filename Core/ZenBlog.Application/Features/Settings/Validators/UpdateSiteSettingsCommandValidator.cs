using FluentValidation;
using ZenBlog.Application.Features.Settings.Commands;

namespace ZenBlog.Application.Features.Settings.Validators;

public class UpdateSiteSettingsCommandValidator : AbstractValidator<UpdateSiteSettingsCommand>
{
    public UpdateSiteSettingsCommandValidator()
    {
        // Bool has no further rules; validator present for pipeline consistency.
    }
}
