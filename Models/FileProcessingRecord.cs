namespace FileProcessingService.Models
{
    public class FileProcessingRecord
    {
        /// <summary>
        /// Unique Identifier of the file proceesed
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();
        /// <summary>
        /// Original filename of uploaded file
        /// </summary>
        public string FileName { get; set; } = string.Empty;
        /// <summary>
        /// Size of file in bytes
        /// </summary>
        public long SizeBytes { get; set; }
        /// <summary>
        /// UTC timestamp of processing
        /// </summary>
        public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Duration taken to process the file
        /// </summary>
        public long DurationMs { get; set; }
        /// <summary>
        /// Did processing succeed?
        /// </summary>
        public bool Succeeded { get; set; }
        /// <summary>
        /// Number of rows processed in the file
        /// </summary>
        public int RowsProcessed { get; set; }
        /// <summary>
        /// Number of Data Rows skipped due to error
        /// </summary>
        public int RowsSkipped  { get; set; }
        /// <summary>
        /// Possible error ,essagewhy attempt to procees failed, a file can still succeed even with skipped rows though. No actual scenario yet.
        /// </summary>
        public string? ErrorMessage { get; set; }
        public ProcessingErrorCode ErrorCode { get; set; } = ProcessingErrorCode.None;
    }
}
