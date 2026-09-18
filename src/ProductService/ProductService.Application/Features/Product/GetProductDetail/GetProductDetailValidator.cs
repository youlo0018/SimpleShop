using FluentValidation;

namespace ProductService.Application.Features.Product.GetProductDetail;

public class GetProductDetailValidator : AbstractValidator<GetProductDetailCommand>
{
    public GetProductDetailValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
