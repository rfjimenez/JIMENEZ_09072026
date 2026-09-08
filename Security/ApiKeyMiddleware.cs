using FileProcessingService.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FileProcessingService.Security
{
    public class ApiKeyMiddleware
    {
        public const string ApiKeyPath = "ApiKey:Value";

        public const string HeaderNamePath = "ApiKey:HeaderName";

        private const string DefaultHeaderName = "X-Api-Key";

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        private static readonly string[] ExemptPathPrefixes = { "/health", "/swagger" };

        private readonly RequestDelegate _next;

        private readonly string _expectedKey;
        private readonly string _headerName;
        private readonly ILogger<ApiKeyMiddleware> _logger;

        public ApiKeyMiddleware(
            RequestDelegate next,
            IConfiguration configuration,
            ILogger<ApiKeyMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _expectedKey = configuration[ApiKeyPath] ?? string.Empty;
            _headerName = configuration[HeaderNamePath] ?? DefaultHeaderName;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (IsExempt(context.Request.Path))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue(_headerName, out var provided))
            {
                _logger.LogWarning(
                    "Rejected {Method} {Path}: missing {Header} header.",
                    context.Request.Method, context.Request.Path, _headerName);

                await WriteUnauthorizedAsync(
                    context,
                    StatusCodes.Status401Unauthorized,
                    "Invalid API Key.");
                return;
            }

            if (!IsValidKey(provided))
            {
                _logger.LogWarning(
                    "Rejected {Method} {Path}: invalid API key.",
                    context.Request.Method, context.Request.Path);
                await WriteUnauthorizedAsync(
                    context,
                    StatusCodes.Status403Forbidden,
                    "Invalid API Key.");
                return;
            }
            await _next(context);
        }

        /// <summary>
        /// Exempted if under the exempted prefixes
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static bool IsExempt(PathString path)
        {
            return ExemptPathPrefixes.Any(prefix =>
                path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsValidKey(string provided)
        {
            if (string.IsNullOrEmpty(_expectedKey) || string.IsNullOrEmpty(provided))
            {
                return false;
            }

            var expectedBytes = Encoding.UTF8.GetBytes(_expectedKey);
            var providedBytes = Encoding.UTF8.GetBytes(provided);

            return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
        }

        private static async Task WriteUnauthorizedAsync(HttpContext context, int statusCode, string message)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new ErrorResponse
            {
                Code = ProcessingErrorCode.Unauthorized,
                Message = message
            }, JsonOptions);
        }
    }
}
