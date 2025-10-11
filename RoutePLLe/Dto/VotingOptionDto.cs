using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPl.Dto
{
    public class VotingOptionDto
    {
        [Required]
        public Guid VotingOptionId { get; set; }

        [Required]
        public Guid VotingSessionId { get; set; }

        [Required, StringLength(500)]
        public string Text { get; set; }

        public int OrderIndex { get; set; }
    }
}
