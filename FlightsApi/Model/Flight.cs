using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlightsApi.Model
{
    public class Flight
    {
        [Key]
        public int Id { get; set; }

        public string Number { get; set; }

        public string Operator { get; set; }

        public virtual List<Passenger> Passengers { get; set; }
    }
}