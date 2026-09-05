using System.Numerics;

namespace FileProcessingService.Models
{
    public class ProcessingReport
    {
        /// <summary>
        /// Total upload attempts
        /// </summary>
        public int TotalFiles { get; set; }
        /// <summary>
        /// Total successful uploads
        /// </summary>
        public int SuccessfulFiles { get; set; }
        /// <summary>
        /// Total failed uploads
        /// </summary>
        public int FailedFiles { get; set; }
        public long TotalRowsProcessed { get; set; }
        public long TotalRowsSkipped { get; set; }
        /// <summary>
        /// total bytes received across all uploads
        /// </summary>
        public long TotalBytesReceived { get; set; }
        /// <summary>
        /// Average processing duration
        /// </summary>
        public double? AverageDurationMs { get; set; }
        /// <summary>
        /// Start time of when the service began tracking (in utc)
        /// </summary>
        public DateTime TrackingSinceUtc { get; set; }
        /// <summary>
        /// count of failures categorized by cause
        /// </summary>
        public Dictionary<ProcessingErrorCode, int> FailuresByCode { get; set; } = new Dictionary<ProcessingErrorCode, int>();
        /// <summary>
        /// List of files that were processed, newest first
        /// </summary>
        public List<FileProcessingRecord> RecentFiles { get; set; } = new List<FileProcessingRecord>();
    }
}
