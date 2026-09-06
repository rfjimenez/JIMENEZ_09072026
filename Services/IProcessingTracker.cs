using FileProcessingService.Models;

namespace FileProcessingService.Services
{
    public interface IProcessingTracker
    {
        void Record(FileProcessingRecord record);

        ProcessingReport GetReport(int recentCount = 20);
    }
}
