namespace ServerSnakesAndLadders.Common
{
    
    public class OperationResult<T>
    {
        public bool IsSuccess { get; }
        public T Data { get; }
        public string ErrorMessage { get; }

        
        private OperationResult(bool isSuccess, T data, string errorMessage)
        {
            IsSuccess = isSuccess;
            Data = data;
            ErrorMessage = errorMessage;
        }

        
        public static OperationResult<T> Success(T data)
        {
            return new OperationResult<T>(true, data, null);
        }

        
        public static OperationResult<T> Failure(string message)
        {
            return new OperationResult<T>(false, default(T), message);
        }
    }
}