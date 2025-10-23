using System;
using System.Threading.Tasks;

namespace EventPl.Services.Interface
{
    public interface IPdfExportService
    {
        Task<byte[]> ExportEventSummaryPdfAsync(Guid eventId);
        Task<byte[]> ExportEventDetailedPdfAsync(Guid eventId);
        Task<byte[]> ExportUserResultPdfAsync(Guid eventId, Guid userId);
        Task<byte[]> ExportEventResultsPdfAsync(Guid eventId, PdfExportOptions options);
        Task<byte[]> ExportCustomMergedWithParticipantsPdfAsync(Guid eventId);
        Task<byte[]> ExportCustomMergedWithParticipantsPdfAsync(Guid eventId, PdfExportOptions participantsOptions);
    }
}

