using FileProcessingService.Models;
using FileProcessingService.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileProcessingService.Controllers
{
    //For accepting CSV uploads and returns an aggregate over a chosen column in the csv file
    public class FilesController : ControllerBase
    {
        /// <summary>
        /// largest upload size accepted in bytes
        /// </summary>
        public const long MaxUploadBytes = 10 * 1024 * 1024;

        private const string CsvExtension = ".csv";

        private readonly ICsvProcessor _processor;
        private readonly IProcessingTracker _tracker;
        private readonly ILogger<FilesController> _logger;

        public FilesController(
            ICsvProcessor processor,
            IProcessingTracker tracker,
            ILogger<FilesController> logger)
        {
            _processor = processor;
            _tracker = tracker;
            _logger = logger;
        }

        [HttpPost("upload")]
        [RequestSizeLimit(MaxUploadBytes)]
        [ProducesResponseType(typeof(ProcessingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status413PayloadTooLarge)]
        public async Task<IActionResult> Upload(
            IFormFile? file,
            [FromQuery] string column = "Amount",
            CancellationToken cancellationToken = default)
        {
            if (file is null || file.Length == 0)
            {
                return RejectBeforeProcessing(
                    file?.FileName ?? string.Empty,
                    file?.Length ?? 0,
                    file is null ? ProcessingErrorCode.NoFileProvided : ProcessingErrorCode.EmptyFile,
                    file is null ?
                        "No file was supplied. Send the file as multipart/form-data under the key 'file'." :
                        "The uploaded file is empty.");
            }

            if (file.Length > MaxUploadBytes)
            {
                return RejectBeforeProcessing(
                    file.FileName,
                    file.Length,
                    ProcessingErrorCode.FileTooLarge,
                    $"The file exceeds the {MaxUploadBytes / (1024 * 1024)}MB Limit.");
            }

            if (!HasCsvExtension(file.FileName))
            {
                return RejectBeforeProcessing(
                    file.FileName,
                    file.Length,
                    ProcessingErrorCode.UnsupportedFileType,
                    $"Only .csv files are supported.");
            }

            if (string.IsNullOrWhiteSpace(column))
            {
                return RejectBeforeProcessing(
                    file.FileName,
                    file.Length,
                    ProcessingErrorCode.ColumnNotFound,
                    $"A column name must be supplied via the 'column' query parameter.");
            }

            _logger.LogInformation(
                "Processing upload {FileName} ({SizeBytes} bytes), aggregating column '{Column}'.",
                file.FileName, file.Length, column);

            ProcessingResult result;

            using (var stream = file.OpenReadStream())
            {
                result = await _processor.ProcessAsync(
                    stream, file.FileName, column, cancellationToken);
            }

            //this is to record every attempt, failures included so report is also able to reflect them
            _tracker.Record(new FileProcessingRecord
            {
                FileName = file.FileName,
                SizeBytes = file.Length,
                DurationMs = result.DurationMs,
                Succeeded = result.Succeeded,
                RowsProcessed = result.RowsProcessed,
                RowsSkipped = result.RowsSkipped,
                ErrorCode = result.ErrorCode,
                ErrorMessage = result.ErrorMessage
            });

            if (!result.Succeeded)
            {
                return StatusCode(
                    MapToStatusCode(result.ErrorCode),
                    new ErrorResponse
                    {
                        Code = result.ErrorCode,
                        Message = result.ErrorMessage ?? "The file could not be processed.",
                        FileName = file.FileName
                    });
            }

            return Ok(result);
        }

        private IActionResult RejectBeforeProcessing(
            string fileName,
            long sizeBytes,
            ProcessingErrorCode code,
            string message)
        {
            _logger.LogWarning(
                "Rejected upload {FileName}: {ErrorCode} - {Message}", fileName, code, message);

            _tracker.Record(new FileProcessingRecord
            {
                FileName = fileName,
                SizeBytes = sizeBytes,
                Succeeded = false,
                ErrorCode = code,
                ErrorMessage = message
            });

            return StatusCode(MapToStatusCode(code),
                new ErrorResponse
                {
                    Code = code,
                    Message = message,
                    FileName = string.IsNullOrEmpty(fileName) ? null : fileName
                });
        }

        private static bool HasCsvExtension(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            return extension.Equals(CsvExtension, StringComparison.OrdinalIgnoreCase);
        }

        private static int MapToStatusCode(ProcessingErrorCode code)
        {
            switch (code)
            {
                case ProcessingErrorCode.FileTooLarge:
                    return StatusCodes.Status413PayloadTooLarge;
                case ProcessingErrorCode.InternalError:
                    return StatusCodes.Status500InternalServerError;
                default:
                    return StatusCodes.Status400BadRequest;
            }
           
        }
    }
}
