using EvenDAL.Models.Classes;
using EventPl.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPl.Factory
{
    public static class DocumentFactory
    {
        public static DocumentDto ToDto(this Document e) =>
            e is null ? null : new DocumentDto
            {
                DocumentId = e.DocumentId,
                EventId = e.EventId,
                AgendaItemId = e.AgendaItemId,
                FileName = e.FileName,
                FilePath = e.FilePath,
                FileSize = e.FileSize,
                FileType = e.FileType,
                UploadedById = e.UploadedById,
                UploadedAt = e.UploadedAt
            };

        public static Document ToEntity(this DocumentDto d) =>
            new Document
            {
                DocumentId = d.DocumentId == Guid.Empty ? Guid.NewGuid() : d.DocumentId,
                EventId = d.EventId,
                AgendaItemId = d.AgendaItemId,
                FileName = d.FileName,
                FilePath = d.FilePath,
                FileSize = d.FileSize,
                FileType = d.FileType,
                UploadedById = d.UploadedById,
                UploadedAt = d.UploadedAt == default ? DateTime.UtcNow : d.UploadedAt
            };
    }
}
