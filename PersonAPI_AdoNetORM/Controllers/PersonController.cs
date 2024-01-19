using Microsoft.AspNetCore.Mvc;
using PersonAPI_AdoNetORM.Services;
using System.Data.SqlClient;
using System.Data;
using PersonAPI_AdoNetORM.Models;
using PersonAPI_AdoNetORM.Validation;
using System.Transactions;

namespace PersonAPI_AdoNetORM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PersonController : ControllerBase
    {
        private readonly IPersonService _personService;
        private readonly IConfiguration _configuration;
        protected IDbConnection Connection => new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        public PersonController(IPersonService personService, IConfiguration configuration)
        {
            _personService = personService;
            _configuration = configuration;
        }

        [HttpPost("CreatePerson")]
        public async Task<IActionResult> CreatePerson(Person newPerson)
        {
            var personValidator = new PersonValidator(_configuration);
            var validatorResults = personValidator.Validate(newPerson);
            if (!validatorResults.IsValid)
            {
                return BadRequest(validatorResults.Errors);
            }
            else
            {
                var result = await _personService.CreatePerson(newPerson);

                if (result == null)
                {
                    return StatusCode(500, new { ErrorMessage = "Failed to create the person in the database!" });
                }
                return Ok(new { SuccessMessage = "Person has successfully created!" });
            }
        }

        [HttpGet("GetPersonById")]
        public async Task<IActionResult> GetPersonById(int personId)
        {
            try
            {
                using (var connection = Connection)
                {
                    connection.Open();
                    var query = "SELECT Persons.Id, FirstName, LastName, Age, Email, Persons.AddressId, " +
                                "Addresses.Country, Addresses.City " +
                                "FROM Persons LEFT JOIN Addresses ON Persons.AddressId = Addresses.Id " +
                                "WHERE Persons.Id = @Id";
                    using (var command = new SqlCommand(query, (SqlConnection)connection))
                    {
                        command.Parameters.AddWithValue("@Id", personId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                await reader.ReadAsync();
                                var person = new Person()
                                {
                                    Id = (int)reader["Id"],
                                    FirstName = reader["FirstName"].ToString(),
                                    LastName = reader["LastName"].ToString(),
                                    Age = (int)reader["Age"],
                                    Email = reader["Email"].ToString(),
                                    AddressId = (int)reader["AddressId"],
                                    PersonAddress = new Address
                                    {
                                        Country = reader["Country"].ToString(),
                                        City = reader["City"].ToString()
                                    }
                                };

                                return Ok(person);
                            }
                            else
                            {
                                return NotFound(new { Message = $"There is no any person by ID: {personId}" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                return StatusCode(500, "Internal Server Error!");
            }
        }

        [HttpGet("GetPersonByCity")]
        public async Task<IActionResult> GetPersonByCity(string city)
        {
            try
            {
                using (var connection = Connection)
                {
                    connection.Open();
                    var query = "SELECT Persons.Id, FirstName, LastName, Age, Email, Persons.AddressId, " +
                                "Addresses.Country, Addresses.City " +
                                "FROM Persons LEFT JOIN Addresses ON Persons.AddressId = Addresses.Id " +
                                "WHERE City = @City";

                    using (var command = new SqlCommand(query, (SqlConnection)connection))
                    {
                        command.Parameters.AddWithValue("@City", city);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var persons = new List<Person>();

                            while (await reader.ReadAsync())
                            {
                                var person = new Person()
                                {
                                    Id = (int)reader["Id"],
                                    FirstName = reader["FirstName"].ToString(),
                                    LastName = reader["LastName"].ToString(),
                                    Age = (int)reader["Age"],
                                    Email = reader["Email"].ToString(),
                                    AddressId = (int)reader["AddressId"],
                                    PersonAddress = new Address
                                    {
                                        Country = reader["Country"].ToString(),
                                        City = reader["City"].ToString()
                                    }
                                };

                                persons.Add(person);
                            }

                            if (persons.Count > 0)
                            {
                                return Ok(persons);
                            }
                            else
                            {
                                return NotFound(new { Message = $"There is no person from: {city}" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                return StatusCode(500, "Internal Server Error!");
            }
        }


        [HttpGet("GetAllPersons")]
        public async Task<IActionResult> GetAllPersons()
        {
            try
            {
                using (var connection = Connection)
                {
                    connection.Open();
                    var query = "SELECT Persons.Id, FirstName, LastName, Age, Email, Persons.AddressId, " +
                                "Addresses.Country, Addresses.City " +
                                "FROM Persons LEFT JOIN Addresses ON Persons.AddressId = Addresses.Id";
                    using (var command = new SqlCommand(query, (SqlConnection)connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                var persons = new List<Person>();

                                while (reader.Read())
                                {
                                    var person = new Person()
                                    {
                                        Id = (int)reader["Id"],
                                        FirstName = reader["FirstName"].ToString(),
                                        LastName = reader["LastName"].ToString(),
                                        Age = (int)reader["Age"],
                                        Email = reader["Email"].ToString(),
                                        AddressId = (int)reader["AddressId"],
                                        PersonAddress = new Address
                                        {
                                            Country = reader["Country"].ToString(),
                                            City = reader["City"].ToString()
                                        }
                                    };
                                    persons.Add(person);
                                }
                                return Ok(persons);
                            }
                            else
                            {
                                return NotFound(new { Message = "There is no person in the database yet!" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                return StatusCode(500, "Internal Server Error!");
            }
        }

        [HttpPut("UpdatePerson")]
        public async Task<IActionResult> UpdatePerson(int personId, Person updatePerson)
        {
            try
            {
                using (var connection = Connection)
                {
                    connection.Open();

                    using (var transaction = connection.BeginTransaction())
                    {
                        using (var existingPersonCmd = new SqlCommand("SELECT * FROM Persons WHERE Id = @personId", (SqlConnection)connection, (SqlTransaction)transaction))
                        {
                            existingPersonCmd.Parameters.AddWithValue("@personId", personId);

                            using (var reader = await existingPersonCmd.ExecuteReaderAsync())
                            {
                                if (!reader.HasRows)
                                {
                                    return BadRequest(new { Error = $"There is no any person by ID: {personId} to update!" });
                                }
                                else
                                {
                                    var personValidator = new PersonValidator(_configuration);
                                    var validatorResults = personValidator.Validate(updatePerson);
                                    if (!validatorResults.IsValid)
                                    {
                                        return BadRequest(validatorResults.Errors);
                                    }
                                    else
                                    {
                                        await _personService.UpdatePerson(personId, updatePerson);
                                        return Ok(new { SuccessMessage = "Person has successfully updated!" });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
            return BadRequest();
        }


        [HttpDelete("DeletePerson")]
        public async Task<IActionResult> DeletePerson(int personId)
        {
            using (var connection = Connection)
            {
                connection.Open();
                using (var transaction = (SqlTransaction)connection.BeginTransaction())
                {
                    using (var command = new SqlCommand("SELECT * FROM Persons WHERE Id = @personId", (SqlConnection)connection, transaction))
                    {
                        command.Parameters.AddWithValue("@personId", personId);

                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return BadRequest(new { Error = $"There is no any person by ID: {personId} to delete!" });
                            }
                            else
                            {
                                await _personService.DeletePerson(personId);
                                return Ok(new { SuccessMessage = "Person has successfully deleted from the database!" });
                            }
                        }
                    }
                }
            }
        }
    }
}