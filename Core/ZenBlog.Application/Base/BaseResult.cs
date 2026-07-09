
using System.Text.Json.Serialization;

namespace ZenBlog.Application.Base
{
    public class BaseResult<T>
    {
        public T? Data { get; set; }
        public IEnumerable<Error> Errors { get; set; } = [];

        [JsonIgnore]
        public ResultStatus Status { get; private set; } = ResultStatus.Success;

        [JsonIgnore]
        public bool IsSuccess => Status == ResultStatus.Success;

        [JsonIgnore]
        public bool IsFailure => !IsSuccess;

        public static BaseResult<T> Success(T data)
        {
            return new BaseResult<T> { Data = data, Status = ResultStatus.Success };
        }

        public static BaseResult<T> Failure()
        {
            return new BaseResult<T>
            {
                Status = ResultStatus.Failure,
                Errors = [new Error { ErrorMessage = "an unexpected error occurred" }]
            };
        }

        public static BaseResult<T> Failure(string errorMessage)
        {
            return new BaseResult<T>
            {
                Status = ResultStatus.Failure,
                Errors = [new Error { ErrorMessage = errorMessage }]
            };
        }

        public static BaseResult<T> Failure(IEnumerable<Error> errors)
        {
            return new BaseResult<T> { Status = ResultStatus.Failure, Errors = errors };
        }

        public static BaseResult<T> NotFound(string message)
        {
            return new BaseResult<T>
            {
                Status = ResultStatus.NotFound,
                Errors = [new Error { ErrorMessage = message }]
            };
        }

        public static BaseResult<T> Unauthorized(string message)
        {
            return new BaseResult<T>
            {
                Status = ResultStatus.Unauthorized,
                Errors = [new Error { ErrorMessage = message }]
            };
        }

        public static BaseResult<T> Forbidden(string message)
        {
            return new BaseResult<T>
            {
                Status = ResultStatus.Forbidden,
                Errors = [new Error { ErrorMessage = message }]
            };
        }
    }

    public class Error
    {
        public string? PropertyName { get; set; }
        public string ErrorMessage { get; set; } = default!;
    }
}
