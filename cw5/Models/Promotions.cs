using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace cw5.Models
{
    public class Promotions{
        [Required]
        public string Studies { get; set; }

        [Required]
        public int Semester { get; set; }
    }
}
