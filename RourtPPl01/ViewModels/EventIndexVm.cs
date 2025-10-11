using System;
using System.Collections.Generic;

namespace EventPresentationlayer.ViewModels
{
    public class EventIndexVm
    {
        public IEnumerable<EventRowVm> Rows { get; set; } = Array.Empty<EventRowVm>();

        // (اختياري) فلاتر بسيطة للمستقبل
        public string? FilterType { get; set; }
        public string? FilterStatus { get; set; }
        public Guid? FilterOrganizationId { get; set; }
    }

    // مهم: نفس EventRowVm اللي الكنترولر بيناديه
    public class EventRowVm
    {
        public Guid EventId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string OrgName { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
