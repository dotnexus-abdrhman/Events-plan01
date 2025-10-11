using EvenDAL.Models.Shared.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvenDAL.Models.Classes
{
    public class Organization
    {
        public Guid OrganizationId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(200)]
        public string NameEn { get; set; }

        public OrganizationType Type { get; set; }

        [MaxLength(500)]
        public string Logo { get; set; }

        [MaxLength(7)] // For hex colors
        public string PrimaryColor { get; set; }

        [MaxLength(7)]
        public string SecondaryColor { get; set; }

        public string Settings { get; set; } // JSON

        [MaxLength(100)]
        public string LicenseKey { get; set; }

        public DateTime LicenseExpiry { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
        public virtual ICollection<Localization> Localizations { get; set; } = new List<Localization>();
    }
}
