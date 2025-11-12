using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AzureSmartCost.Shared.Models
{
    public class ApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new List<string>();

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        // Success factory method
        public static ApiResponse<T> CreateSuccess(T data, string message = "Operation completed successfully")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message
            };
        }

        // Error factory method
        public static ApiResponse<T> CreateError(string message, params string[] errors)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = new List<string>(errors)
            };
        }

        // Error factory method with exception
        public static ApiResponse<T> CreateError(Exception exception, string message = "An error occurred")
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = new List<string> { exception.Message }
            };
        }
    }
}
