namespace FileProcessingService.Models
{
    public class ErrorResponse
    {
        /// <summary>
        ///  Error codes to represent File Processing Error Scenarios
        /// </summary>
        public ProcessingErrorCode Code { get; set; } = ProcessingErrorCode.InternalError;
        /// <summary>
        /// Human readable explanation
        /// </summary>
        public string Message { get; set; } = string.Empty;
        /// <summary>
        /// Name of the file that was rejected upon upload
        /// </summary>
        public string? FileName { get; set; }
        /// <summary>
        /// UTC time the failure was recorded
        /// </summary>
        public DateTime? TimeStampUtc { get; set; } = DateTime.UtcNow;

    }
}
