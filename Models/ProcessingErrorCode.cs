namespace FileProcessingService.Models
{
    public enum ProcessingErrorCode
    {
        None = 0,  
        Unauthorized,
        NoFileProvided,
        EmptyFile,
        FileTooLarge,
        UnsupportedFileType,
        MalformedCsv,
        ColumnNotFound,
        NoUsableRows,
        InternalError
    }
}
