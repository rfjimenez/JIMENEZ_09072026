using FileProcessingService.Models;

namespace FileProcessingService.Services
{
    public class InMemoryProcessingTracker : IProcessingTracker
    {
        private const int HistoryLimit = 100;

        private readonly object _gate = new object();

        private readonly Queue<FileProcessingRecord> _history = new Queue<FileProcessingRecord>();
        private readonly Dictionary<ProcessingErrorCode, int> _failuresByCode = new Dictionary<ProcessingErrorCode, int> { };

        private readonly DateTime _trackingSinceUtc = DateTime.UtcNow;
        private readonly ILogger<InMemoryProcessingTracker> _logger;

        private int _totalFiles;
        private int _successfulFiles;
        private int _failedFiles;
        private long _totalRowsProcessed;
        private long _totalRowsSkipped;
        private long _totalBytesReceived;
        private long _totalDurationMs;

        public InMemoryProcessingTracker(ILogger<InMemoryProcessingTracker> logger)
        {
            _logger = logger;
        }

        public void Record(FileProcessingRecord record)
        {
            ArgumentNullException.ThrowIfNull(record);

            lock (_gate)
            {
                _totalFiles++;
                _totalRowsProcessed += record.RowsProcessed;
                _totalRowsSkipped += record.RowsSkipped;
                _totalBytesReceived += record.SizeBytes;
                _totalDurationMs += record.DurationMs;

                if (record.Succeeded)
                {
                    _successfulFiles++;
                }
                else
                {
                    _failedFiles++;
                    _failuresByCode.TryGetValue(record.ErrorCode, out var count);
                    _failuresByCode[record.ErrorCode] = count + 1;
                }

                _history.Enqueue(record);

                while (_history.Count > HistoryLimit)
                {
                    _history.Dequeue();
                }
            }

            _logger.LogInformation(
                "Tracked {FileName}: succeeded={Succeeded}, rows={RowsProcessed}, skipped={RowsSkipped}, {DurationMs}ms.",
                record.FileName, record.Succeeded, record.RowsProcessed, record.RowsSkipped, record.DurationMs);
        }

        public ProcessingReport GetReport(int recentCount = 20)
        {
            if (recentCount < 0)
            {
                recentCount = 0;
            }

            lock (_gate)
            {
                //history is a queue, we need to reverse to get the most recent one
                var recent = _history.Reverse().Take(recentCount).ToList();

                return new ProcessingReport
                {
                    TotalFiles = _totalFiles,
                    SuccessfulFiles = _successfulFiles,
                    FailedFiles = _failedFiles,
                    TotalRowsProcessed = _totalRowsProcessed,
                    TotalRowsSkipped = _totalRowsSkipped,
                    TotalBytesReceived = _totalBytesReceived,
                    AverageDurationMs = _totalFiles == 0 ?
                        null : (double)_totalDurationMs / _totalFiles,
                    TrackingSinceUtc = _trackingSinceUtc,
                    FailuresByCode = new Dictionary<ProcessingErrorCode, int>(_failuresByCode),
                    RecentFiles = recent
                };
            }
        }
    }
}
