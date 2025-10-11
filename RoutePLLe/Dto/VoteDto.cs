using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPl.Dto
{
    public class VoteDto
    {
        [Required]
        public Guid VoteId { get; set; }

        [Required]
        public Guid VotingSessionId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public Guid? VotingOptionId { get; set; }

        [StringLength(1000)]
        public string CustomAnswer { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime VotedAt { get; set; }
    }
}
