namespace Communicator
{
    public class CommandResponse
    {
        public int RequestId { get; }
        public string RawJson { get; }

        public CommandResponse(int requestId, string rawJson)
        {
            RequestId = requestId;
            RawJson = rawJson;
        }
    }
}