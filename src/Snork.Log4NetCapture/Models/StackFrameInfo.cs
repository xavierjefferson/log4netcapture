namespace Snork.Log4NetCapture.Models
{
    public class StackFrameInfo
    {
        public StackFrameInfo(string message, object parent)
        {
            Message = message;
            Parent = parent;
        }

        public string Message { get; }
        public object Parent { get; }
    }
}