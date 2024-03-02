namespace O2Client
{
    public class ClientException : Exception
    {
        public int ErrorCode { get; private set; }

        public ClientException(int errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
