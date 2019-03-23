using Qmmands;

namespace Fox.Entities
{
    public sealed class FoxResult : CommandResult
    {
        public override bool IsSuccessful { get; }

        public string Message { get; }

        public FoxResult(bool isSuccessful, string message = null)
        {
            IsSuccessful = isSuccessful;
            Message = message;
        }
    }
}
