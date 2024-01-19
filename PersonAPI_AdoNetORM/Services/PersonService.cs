using PersonAPI_AdoNetORM.Models;
using PersonAPI_AdoNetORM.Validation;
using System.Data;
using System.Data.SqlClient;

namespace PersonAPI_AdoNetORM.Services
{
    public interface IPersonService
    {
        Task<Person> CreatePerson(Person newPerson);
        Task<bool> UpdatePerson(int personId,Person updatePerson);
        Task<bool> DeletePerson(int personId);
    }
    public class PersonService : IPersonService
    {
        private readonly IConfiguration _configuration;
        public PersonService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        protected IDbConnection Connection => new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

        public async Task<Person> CreatePerson(Person person)
        {
            try
            {
                var personValidator = new PersonValidator(_configuration);
                var validatorResults = await personValidator.ValidateAsync(person);
                if (!validatorResults.IsValid)
                {
                    return null;
                }
                using (var connection = Connection)
                {
                    connection.Open();
                    using (var command = new SqlCommand("INSERT INTO Addresses (Country, City) VALUES (@Country, @City); SELECT SCOPE_IDENTITY()", (SqlConnection)connection))
                    {
                        command.Parameters.AddWithValue("@Country", person.PersonAddress.Country);
                        command.Parameters.AddWithValue("@City", person.PersonAddress.City);
                        person.PersonAddress.Id = Convert.ToInt32(command.ExecuteScalar());
                        person.AddressId = person.PersonAddress.Id;
                    }
                    using (var command = new SqlCommand("INSERT INTO Persons (FirstName, LastName, Email, Age, AddressId) VALUES (@FirstName, @LastName, @Email, @Age, @AddressId)", (SqlConnection)connection))
                    {
                        command.Parameters.AddWithValue("@FirstName", person.FirstName);
                        command.Parameters.AddWithValue("@LastName", person.LastName);
                        command.Parameters.AddWithValue("@Email", person.Email);
                        command.Parameters.AddWithValue("@Age", person.Age);
                        command.Parameters.AddWithValue("@AddressId", person.AddressId);
                        command.ExecuteNonQuery();
                        
                    }
                }

                return person;
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Exception: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        public async Task<bool> DeletePerson(int personId)
        {
            try
            {
                using (var connection = Connection)
                {
                    connection.Open();
                    using (var transaction = (SqlTransaction)connection.BeginTransaction())
                    {
                        try
                        {
                            using (var command = new SqlCommand("SELECT * FROM Persons WHERE Id = @personId", (SqlConnection)connection, transaction))
                            {
                                command.Parameters.AddWithValue("@personId", personId);

                                using (var reader = command.ExecuteReader())
                                {
                                    if (!reader.HasRows)
                                    {
                                        return false;
                                    }
                                }
                            }
                            using (var deletePersonCommand = new SqlCommand("DELETE FROM Persons WHERE Id = @personId", (SqlConnection)connection, transaction))
                            {
                                deletePersonCommand.Parameters.AddWithValue("@personId", personId);
                                deletePersonCommand.ExecuteNonQuery();
                            }
                            using (var deleteAddressCommand = new SqlCommand("DELETE FROM Addresses WHERE Id = @addressId", (SqlConnection)connection, transaction))
                            {
                                deleteAddressCommand.Parameters.AddWithValue("@addressId", personId);
                                deleteAddressCommand.ExecuteNonQuery();
                            }
                            transaction.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Transaction Exception: {ex.Message}");
                            transaction.Rollback();
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdatePerson(int personId, Person updatePerson)
        {
            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            var selectCommand = new SqlCommand("SELECT * FROM Persons WHERE Id = @Id", connection, transaction);
                            selectCommand.Parameters.AddWithValue("@Id", personId);
                            var existingPersonReader = await selectCommand.ExecuteReaderAsync();

                            Person existingPerson = null;
                            if (existingPersonReader.Read())
                            {
                                existingPerson = new Person
                                {
                                    Id = (int)existingPersonReader["Id"],
                                    FirstName = existingPersonReader["FirstName"].ToString(),
                                    LastName = existingPersonReader["LastName"].ToString(),
                                    Age = (int)existingPersonReader["Age"],
                                    Email = existingPersonReader["Email"].ToString(),
                                    AddressId = (int)existingPersonReader["AddressId"]
                                };
                            }

                            existingPersonReader.Close();

                            if (existingPerson == null)
                            {
                                return false;
                            }
                            var personValidator = new PersonValidator(_configuration);
                            var validatorResults = personValidator.Validate(updatePerson);
                            if (!validatorResults.IsValid)
                            {
                                return false;
                            }
                            var updatePersonsCommand = new SqlCommand("UPDATE Persons SET FirstName = @FirstName, LastName = @LastName, Age = @Age, Email = @Email WHERE Id = @Id", connection, transaction);
                            updatePersonsCommand.Parameters.AddWithValue("@FirstName", updatePerson.FirstName);
                            updatePersonsCommand.Parameters.AddWithValue("@LastName", updatePerson.LastName);
                            updatePersonsCommand.Parameters.AddWithValue("@Age", updatePerson.Age);
                            updatePersonsCommand.Parameters.AddWithValue("@Email", updatePerson.Email);
                            updatePersonsCommand.Parameters.AddWithValue("@Id", personId);
                            await updatePersonsCommand.ExecuteNonQueryAsync();

                            var updateAddressesCommand = new SqlCommand("UPDATE Addresses SET Country = @Country, City = @City WHERE Id = @Id", connection, transaction);
                            updateAddressesCommand.Parameters.AddWithValue("@Country", updatePerson.PersonAddress.Country);
                            updateAddressesCommand.Parameters.AddWithValue("@City", updatePerson.PersonAddress.City);
                            updateAddressesCommand.Parameters.AddWithValue("@Id", existingPerson.AddressId);
                            await updateAddressesCommand.ExecuteNonQueryAsync();

                            transaction.Commit();

                            return true;
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            return false;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}
