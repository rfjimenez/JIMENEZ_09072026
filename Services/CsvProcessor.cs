using CsvHelper;
using CsvHelper.Configuration;
using FileProcessingService.Models;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;

namespace FileProcessingService.Services
{
    public class CsvProcessor : ICsvProcessor
    {
        private const int MaxWarnings = 100;

        private const int MaxValueLength = 50;

        private readonly ILogger<CsvProcessor> _logger;

        public CsvProcessor(ILogger<CsvProcessor> logger)
        {
            _logger = logger;
        }

        public async Task<ProcessingResult> ProcessAsync(
            Stream stream,
            string fileName,
            string columnName,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentException.ThrowIfNullOrWhiteSpace(columnName);

            var stopwatch = Stopwatch.StartNew();

            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                TrimOptions = TrimOptions.Trim,
                IgnoreBlankLines = true,
                MissingFieldFound = null,
                HeaderValidated = null
            };

            var warnings = new List<string>();
            var rowsProcessed = 0;
            var rowsSkipped = 0;
            decimal sum = 0m;

            using (var reader = new StreamReader(stream, leaveOpen: true))
            using (var csv = new CsvReader(reader, configuration))
            {
                var (resolvedColumn, headerFailure) = await TryReadHeaderAsync(csv, columnName, fileName, cancellationToken);

                if (headerFailure is not null)
                {
                    stopwatch.Stop();
                    headerFailure.DurationMs = stopwatch.ElapsedMilliseconds;
                    return headerFailure;

                }

                while (await csv.ReadAsync())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    //header is always line 1, first data is from line 2
                    var lineNumber = csv.Context.Parser?.Row ?? rowsProcessed + rowsSkipped + 2;
                    var raw = csv.GetField(resolvedColumn);

                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        rowsSkipped++;
                        AddWarning(warnings, rowsSkipped,
                            $"Line {lineNumber}: column '{columnName}' is empty, row skipped.");
                        continue;
                    }

                    if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                    {
                        rowsSkipped++;
                        AddWarning(warnings, rowsSkipped,
                            $"Line {lineNumber}: value '{Truncate(raw)}' is not numeric, row skipped.");
                    }

                    sum += value;
                    rowsProcessed++;
                }
            }

            stopwatch.Stop();

            if (rowsSkipped > MaxWarnings)
            {
                warnings.Add(
                    $"... and {rowsSkipped - MaxWarnings:N0} more rows skipped (warning list capped at {MaxWarnings}).");
            }

            if (rowsProcessed == 0)
            {
                _logger.LogWarning(
                    "{FileName}: no usable numeric values in column '{Column}'.",
                    fileName, columnName);

                return Failure(
                    ProcessingErrorCode.NoUsableRows,
                    $"No rows contained a usable numeric value in column '{columnName}'.",
                    fileName, columnName, stopwatch.ElapsedMilliseconds, rowsSkipped, warnings);
            }

            _logger.LogInformation(
                "Processed {FileName}: {RowsProcessed} rows aggregated, {RowsSkipped} skipped, in {DurationMs}ms.",
                fileName, rowsProcessed, rowsSkipped, stopwatch.ElapsedMilliseconds);

            if (rowsSkipped > 0)
            {
                _logger.LogWarning(
                    "{FileName}: {RowsSkipped} row(s) skipped while aggregating column '{Column}'.",
                    fileName, rowsSkipped, columnName);
            }

            return new ProcessingResult()
            {
                Succeeded = true,
                FileName = fileName,
                Column = columnName,
                RowsProcessed = rowsProcessed,
                RowsSkipped = rowsSkipped,
                Sum = sum,
                Average = sum / rowsProcessed,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Warnings = warnings
            };
        }

        private async Task<(string ResolvedColumn, ProcessingResult? Failure)> TryReadHeaderAsync(
            CsvReader csv,
            string columnName,
            string fileName,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool hasHeader;

            try
            {
                hasHeader = await csv.ReadAsync();
                if (hasHeader)
                {
                    csv.ReadHeader();
                }
            }
            catch (CsvHelperException ex)
            {
                _logger.LogWarning(ex, "Malformed CSV header in {FileName}.", fileName);
                return (string.Empty, Failure(
                    ProcessingErrorCode.MalformedCsv,
                    "The file could not be read as CSV.",
                    fileName, columnName));
            }

            var headers = csv.HeaderRecord;
            if (!hasHeader || headers is null || headers.Length == 0)
            {
                _logger.LogWarning("{FileName}: empty file or missing header row.", fileName);
                return (string.Empty, Failure(
                    ProcessingErrorCode.MalformedCsv,
                    "The file is empty or contains no header row.",
                    fileName, columnName));
            }

            var resolved = headers.FirstOrDefault(h => string.Equals(h?.Trim(), columnName, StringComparison.OrdinalIgnoreCase));

            if (resolved is null)
            {
                _logger.LogWarning("{FileName}: column '{Column}' not found in header.", fileName, columnName);
                return (string.Empty, Failure(
                    ProcessingErrorCode.ColumnNotFound,
                    $"Column '{columnName}' was not found. Available columns: {string.Join(", ", headers)}.",
                    fileName, columnName));
            }

            return ( resolved, null);
        }

        private static ProcessingResult Failure(
            ProcessingErrorCode code,
            string message,
            string fileName,
            string columnName,
            long durationMs = 0,
            int rowsSkipped = 0,
            List<string>? warnings = null)
        {
            return new ProcessingResult()
            {
                Succeeded = false,
                ErrorCode = code,
                ErrorMessage = message,
                FileName = fileName,
                Column = columnName,
                RowsSkipped = rowsSkipped,
                DurationMs = durationMs,
                Warnings = warnings ?? new List<string>()
            };
        }

        private static void AddWarning(List<string> warnings, int rowsSkipped, string message)
        {
            if (rowsSkipped <= MaxWarnings)
            {
                warnings.Add(message);
            }
        }

        private static string Truncate(string value)
        {
            if (value.Length <= MaxValueLength)
            {
                return value;
            }

            return value.Substring(0, MaxValueLength) + "...";
        }
    }
}
