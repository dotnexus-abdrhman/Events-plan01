using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace EventPresentationlayer.ViewModels
{
    public class TableVm
    {
        [Display(Name = "عنوان الجدول")]
        [StringLength(200)]
        public string? Title { get; set; }

        [Display(Name = "وصف مختصر")]
        [StringLength(500)]
        public string? Description { get; set; }

        // يمثل أعمدة/صفوف الجدول بصيغة JSON داخل النموذج
        // الشكل المتوقع:
        // { "columns": [{"name":"Col1","type":"Text|Number|Currency|Date"}, ...],
        //   "rows": [["v1","v2",...], ...],
        //   "hasHeader": true|false }
        public string? Json { get; set; }

        // لعرض القراءة فقط للمستخدم
        public bool HasHeader { get; set; }
        public List<List<string>> Rows { get; set; } = new();
    }
}

