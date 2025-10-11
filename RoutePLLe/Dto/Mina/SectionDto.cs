using System;
using System.Collections.Generic;

namespace EventPl.Dto.Mina
{
    /// <summary>
    /// DTO للبند (Section) مع القرارات
    /// </summary>
    public class SectionDto
    {
        public Guid SectionId { get; set; }
        public Guid EventId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // القرارات داخل البند
        public List<DecisionDto> Decisions { get; set; } = new();
    }

    /// <summary>
    /// DTO للقرار (Decision) مع العناصر
    /// </summary>
    public class DecisionDto
    {
        public Guid DecisionId { get; set; }
        public Guid SectionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // عناصر القرار المرقّمة
        public List<DecisionItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// DTO لعنصر القرار (DecisionItem)
    /// </summary>
    public class DecisionItemDto
    {
        public Guid DecisionItemId { get; set; }
        public Guid DecisionId { get; set; }
        public string Text { get; set; } = string.Empty;
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

