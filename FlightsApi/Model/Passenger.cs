using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightsApi.Model
{
    public class Passenger
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("FlightForeignKey")]
        public virtual Flight Flight { get; set; }
    }
}
