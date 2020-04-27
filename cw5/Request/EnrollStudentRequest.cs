using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace cw5.Request
{
    public class EnrollStudentRequest {

        [Required]
        [RegularExpression("^s[0-9]+$")]
        public string IndexNumber { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        [Required]
        public string Birthdate { get; set; }

        [Required]
        public string Studies { get; set; }
    }
}
