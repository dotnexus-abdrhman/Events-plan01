using System;
using System.Collections.Generic;

namespace EventPl.Dto.Mina
{
    /// <summary>
    /// DTO للجدول المرن (TableBlock)
    /// </summary>
    public class TableBlockDto
    {
        public Guid TableBlockId { get; set; }
        public Guid EventId { get; set; }
        public Guid? SectionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool HasHeader { get; set; }
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; }

        // البيانات المرنة (Rows/Cells)
        public TableDataDto? TableData { get; set; }
    }

    /// <summary>
    /// بيانات الجدول المرنة (Case-Insensitive)
    /// </summary>
    public class TableDataDto
    {
        public List<TableRowDto> Rows { get; set; } = new();
    }

    public class TableRowDto
    {
        public List<string> Cells { get; set; } = new();
    }
}

