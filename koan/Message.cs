using System.Diagnostics.Contracts;

namespace Koan
{
    public abstract class Message
    {
    }

    internal interface IMessageHandler
    {
        string HandlerName { get; }
        bool TryHandle(Message message);
        bool IsSame(object handler);
    }

    public interface IHandle<T> where T : Message
    {
        void Handle(T message);
    }

    public interface ISubscriber
    {
        void Subscribe<T>(IHandle<T> handler) where T : Message;
        void Unsubscribe<T>(IHandle<T> handler) where T : Message;
    }
    public interface IPublisher
    {
        void Publish(Message message);
    }



    public interface IBus : IPublisher, ISubscriber
    {
        string Name { get; }
    }

    public sealed class MessageHandler<T> : IMessageHandler where T : Message
    {

        readonly IHandle<T> _handler;
        public MessageHandler(IHandle<T> handler, string handlerName)
        {
            Contract.Requires(handler != null);
            HandlerName = handlerName ?? "";
            _handler = handler;
        }

        public string HandlerName { get; private set; }
        public bool TryHandle(Message message)
        {
            var msg = message as T;

            if (msg != null)
            {
                _handler.Handle(msg);
                return true;
            }
            return false;
        }

        public bool IsSame(object handler)
        {
            return ReferenceEquals(_handler, handler);
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(HandlerName) ? _handler.ToString() : HandlerName;
        }
    }

    public class PublishEnvelope : IEnvelope
    {
        private readonly IPublisher _publisher;

        public PublishEnvelope(IPublisher publisher)
        {
            _publisher = publisher;
        }

        public void ReplyWith<T>(T message) where T : Message
        {
            _publisher.Publish(message);
        }
    }



    public interface IEnvelope
    {
        void ReplyWith<T>(T message) where T : Message;
    }

}