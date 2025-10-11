using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPl.Dto
{
    public class ModuleDto
    {
        [Required]
        public Guid ModuleId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        public bool IsActive { get; set; }

        [StringLength(4000)]
        public string RequiredFeatures { get; set; } // JSON

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; }
    }
}
