using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPl.Dto
{
    public class AgendaItemDto
    {
        [Required]
        public Guid AgendaItemId { get; set; }

        [Required]
        public Guid EventId { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; }

        public string Description { get; set; }

        [Range(0, int.MaxValue)]
        public int OrderIndex { get; set; }

        [Range(0, 1440)]
        public int EstimatedDuration { get; set; } // minutes

        [StringLength(50)]
        public string TypeName { get; set; }       // Discussion / Voting / Presentation / Break

        public bool RequiresVoting { get; set; }

        public Guid? PresenterId { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; }
    }
}
