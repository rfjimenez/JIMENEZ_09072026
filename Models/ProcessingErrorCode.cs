namespace FileProcessingService.Models
{
    public enum ProcessingErrorCode
    {
        None = 0,  
        NoFileProcided,
        EmptyFile,
        FileTooLarge,
        UnsupportedFileType,
        MalformedCsv,
        ColumnNotFound,
        NoUsableRows,
        InternalError
    }
}
