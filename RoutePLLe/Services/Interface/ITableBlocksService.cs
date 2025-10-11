using EventPl.Dto.Mina;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventPl.Services.Interface
{
    /// <summary>
    /// خدمة إدارة الجداول المرنة (TableBlocks)
    /// </summary>
    public interface ITableBlocksService
    {
        /// <summary>
        /// الحصول على جميع جداول الحدث
        /// </summary>
        Task<List<TableBlockDto>> GetEventTablesAsync(Guid eventId);
        
        /// <summary>
        /// الحصول على جدول محدد
        /// </summary>
        Task<TableBlockDto?> GetTableByIdAsync(Guid tableBlockId);
        
        /// <summary>
        /// إنشاء جدول جديد
        /// </summary>
        Task<TableBlockDto> CreateTableAsync(TableBlockDto dto);
        
        /// <summary>
        /// تحديث جدول (مع معالجة JSON Case-Insensitive)
        /// </summary>
        Task<bool> UpdateTableAsync(TableBlockDto dto);
        
        /// <summary>
        /// حذف جدول
        /// </summary>
        Task<bool> DeleteTableAsync(Guid tableBlockId);
    }
}

