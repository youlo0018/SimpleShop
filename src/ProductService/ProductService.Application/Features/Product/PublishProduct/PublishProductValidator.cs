using FluentValidation;

namespace ProductService.Application.Features.Product.PublishProduct;

public class PublishProductValidator : AbstractValidator<PublishProductCommand>
{
    public PublishProductValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
