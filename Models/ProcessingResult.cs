using System.Diagnostics.Eventing.Reader;

namespace FileProcessingService.Models
{
    /// <summary>
    /// per file result
    /// </summary>
    public class ProcessingResult
    {
        /// <summary>
        /// if aggregated successfully
        /// </summary>
        public bool Succeeded { get; set; }
        /// <summary>
        /// why the processing failed
        /// this will be mapped to a corresponding http status code
        /// </summary>
        public ProcessingErrorCode ErrorCode { get; set; } = ProcessingErrorCode.None;
        public string? ErrorMessage { get; set; }
        /// <summary>
        /// Original Name of the uploaded file
        /// </summary>
        public string FileName { get; set; } = string.Empty;
        /// <summary>
        /// Name of the column the aggregate was calculated over
        /// </summary>
        public string Column { get; set; } = string.Empty;
        /// <summary>
        /// number of data rows that were parsed successfully and added in the aggregate
        /// </summary>
        public int RowsProcessed { get; set; }
        /// <summary>
        /// Number of date rows skipped due to issues with target column
        /// </summary>
        public int RowsSkipped { get; set; }
        /// <summary>
        /// ave of rows in target column
        /// </summary>
        public decimal? Average {  get; set; }
        /// <summary>
        /// sum of the target column's rows
        /// </summary>
        public decimal Sum { get; set; }
        /// <summary>
        /// time taken to process the file
        /// </summary>
        public long DurationMs { get; set; }
        /// <summary>
        /// warnings while processing, won't skipp a file if there is a problematic row
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
