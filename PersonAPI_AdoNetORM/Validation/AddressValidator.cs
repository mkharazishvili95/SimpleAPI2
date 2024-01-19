using FluentValidation;
using PersonAPI_AdoNetORM.Models;

namespace PersonAPI_AdoNetORM.Validation
{
    public class AddressValidator : AbstractValidator<Address>
    {
        public AddressValidator() 
        {
            RuleFor(a => a.Country).NotEmpty().WithMessage("Enter your Country!");
            RuleFor(a => a.City).NotEmpty().WithMessage("Enter your City!");
        }
    }
}
