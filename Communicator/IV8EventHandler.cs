using Communicator.Messages;

namespace Communicator
{
    public interface IV8EventHandler
    {
        void Raise<TEvent>(TEvent e) where TEvent : IV8EventParameters;
    }
}