using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPl.Dto
{
    public class OrganizationDto
    {
        [Required]
        public Guid OrganizationId { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; }

        [StringLength(200)]
        public string NameEn { get; set; }

        // عرض نوع المنظمة
        [StringLength(50)]
        public string TypeName { get; set; }   // Government / Private / NonProfit / Other

        [StringLength(500)]
        public string Logo { get; set; }

        // HEX #RRGGBB
        [RegularExpression("^#([A-Fa-f0-9]{6})$", ErrorMessage = "لون غير صالح. استخدم #RRGGBB")]
        [StringLength(7)]
        public string PrimaryColor { get; set; }

        [RegularExpression("^#([A-Fa-f0-9]{6})$", ErrorMessage = "لون غير صالح. استخدم #RRGGBB")]
        [StringLength(7)]
        public string SecondaryColor { get; set; }

        [StringLength(4000)]
        public string Settings { get; set; }   // JSON

        [StringLength(100)]
        public string LicenseKey { get; set; }

        [DataType(DataType.Date)]
        public DateTime? LicenseExpiry { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; }

        public int Type { get; set; }

        public bool IsActive { get; set; }

        // محسوبة للعرض
        public bool? IsLicenseExpired =>
            LicenseExpiry.HasValue ? LicenseExpiry.Value.Date < DateTime.UtcNow.Date : (bool?)null;
    }
}
