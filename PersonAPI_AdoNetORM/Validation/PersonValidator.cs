using FluentValidation;
using Microsoft.Extensions.Configuration;
using PersonAPI_AdoNetORM.Models;
using System.Data;
using System.Data.SqlClient;

namespace PersonAPI_AdoNetORM.Validation
{
    public class PersonValidator : AbstractValidator<Person>
    {
        private readonly IConfiguration _configuration;
        public PersonValidator(IConfiguration configuration) 
        {
            _configuration = configuration;

            RuleFor(p => p.FirstName).NotEmpty().WithMessage("Enter your FirstName!");
            RuleFor(p => p.LastName).NotEmpty().WithMessage("Enter your LastName!");
            RuleFor(p => p.Age).NotEmpty().WithMessage("Enter your Age!")
                .GreaterThanOrEqualTo(18).WithMessage("Your age must be 18 or more to register!");
            RuleFor(p => p.Email).NotEmpty().WithMessage("Enter your Email address!")
                .EmailAddress().WithMessage("Enter your Valid Email address!")
                .Must(BeUniqueEmail).WithMessage("Email address already exists. Try another!");
            RuleFor(p => p.PersonAddress).SetValidator(new AddressValidator());
        }
        private IDbConnection Connection => new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        private bool BeUniqueEmail(string email)
        {
            using (var connection = Connection)
            {
                connection.Open();

                using (var command = new SqlCommand("SELECT COUNT(*) FROM Persons WHERE Email = @Email", (SqlConnection)connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    var result = (int)command.ExecuteScalar();
                    return result == 0;
                }
            }
        }
    }
}
