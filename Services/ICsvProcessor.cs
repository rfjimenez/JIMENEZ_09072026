using FileProcessingService.Models;

namespace FileProcessingService.Services
{
    public interface ICsvProcessor
    {
        Task<ProcessingResult> ProcessAsync(
            Stream stream,
            string fileName,
            string columnName,
            CancellationToken cancellationToken = default);
    }
}
