using FluentValidation;

namespace MerchantPlatformService.Application.Features.PlatformConfigs.Save;

public sealed class SavePlatformConfigValidator : AbstractValidator<SavePlatformConfigCommand>
{
    public SavePlatformConfigValidator()
    {
        RuleFor(x => x.ConfigKey).NotEmpty().Matches("^[a-zA-Z][a-zA-Z0-9.:_-]{1,63}$");
        RuleFor(x => x.ConfigValue).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(255);
        RuleFor(x => x.ConfigType).InclusiveBetween(0, 2);
    }
}
