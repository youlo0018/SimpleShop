using FluentValidation;

namespace MerchantPlatformService.Application.Features.Platforms.Create;

public sealed class CreatePlatformValidator : AbstractValidator<CreatePlatformCommand>
{
    public CreatePlatformValidator()
    {
        RuleFor(x => x.PlatformCode).NotEmpty().Matches("^[a-zA-Z][a-zA-Z0-9_-]{2,31}$");
        RuleFor(x => x.PlatformName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.ContactEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.DefaultCommissionRate).InclusiveBetween(0, 100);
    }
}
