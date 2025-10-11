using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPl.Dto
{
    public class LocalizationDto
    {
        [Required]
        public Guid LocalizationId { get; set; }

        [Required]
        public Guid OrganizationId { get; set; }

        [Required, StringLength(100)]
        public string Key { get; set; }

        [Required]
        public string Value { get; set; }

        [Required, StringLength(5)]
        public string Language { get; set; } // "ar", "en"
    }
}
