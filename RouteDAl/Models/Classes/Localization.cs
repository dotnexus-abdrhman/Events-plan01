using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvenDAL.Models.Classes
{
    public class Localization
    {
        public Guid LocalizationId { get; set; }
        public Guid OrganizationId { get; set; }

        [Required, MaxLength(100)]
        public string Key { get; set; }

        [Required]
        public string Value { get; set; }

        [Required, MaxLength(5)]
        public string Language { get; set; } // "ar", "en"

        // Navigation Properties
        public virtual Organization Organization { get; set; }
    }
}
