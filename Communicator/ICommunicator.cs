using System.Threading;
using System.Threading.Tasks;
using Communicator.Messages;

namespace Communicator
{
    public interface ICommunicator
    {
        Task Connect(CancellationToken cancellationToken);
        Task<string> SendCommand(string method, object parameters = null);
        Task<TEvent> WaitForEventAsync<TEvent>(CancellationToken token) where TEvent : IV8EventParameters;
    }
}