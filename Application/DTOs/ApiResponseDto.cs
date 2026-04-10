using System.Text.Json.Serialization;

namespace Rapsodia.Application.DTOs
{
    /// <summary>Envelope alinhado ao front (success / data / message).</summary>
    public class ApiResponseDto<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        public static ApiResponseDto<T> Ok(T data) =>
            new() { Success = true, Data = data };

        public static ApiResponseDto<T> Fail(string message) =>
            new() { Success = false, Message = message };
    }
}
