using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlClient;

namespace PersonAPI_AdoNetORM.Models
{
    public class Person
    {

        [Key]
        public int Id { get; set; }
        public string FirstName {  get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int Age {  get; set; }
        public string Email { get; set; } = string.Empty;
        public Address PersonAddress { get; set; }

        [ForeignKey("PersonAddress")]
        public int AddressId {  get; set; }
    }
}
