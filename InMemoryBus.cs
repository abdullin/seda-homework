using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Koan
{
    public sealed class InMemoryBus : IBus, IPublisher, ISubscriber, IHandle<Message>
    {

        private readonly Dictionary<Type, List<IMessageHandler>> _typeLookup = new Dictionary<Type, List<IMessageHandler>>();

        public void Subscribe<T>(IHandle<T> handler) where T : Message
        {
            Contract.Requires(handler != null);

            List<IMessageHandler> handlers;
            var type = typeof(T);
            if (!_typeLookup.TryGetValue(type, out handlers))
            {
                _typeLookup.Add(type, handlers = new List<IMessageHandler>());
            }
            if (!handlers.Any(h => h.IsSame(handler)))
            {
                handlers.Add(new MessageHandler<T>(handler, handler.GetType().Name));
            }
        }

        public void Unsubscribe<T>(IHandle<T> handler) where T : Message
        {
            Contract.Requires(handler != null);
            List<IMessageHandler> list;
            if (_typeLookup.TryGetValue(typeof(T),out list))
            {
                list.RemoveAll(x => x.IsSame(handler));
            }
        }

        public string Name { get; private set; }

        public InMemoryBus(string name)
        {
            Name = name;
        }

        public void Publish(Message message)
        {
            Contract.Requires(message != null);
            DispatchByType(message);
        }

        public void Handle(Message message)
        {
            Contract.Requires(message != null);
            DispatchByType(message);
        }

        void DispatchByType(Message message)
        {
            var type = message.GetType();
            do
            {
                DispatchByType(message, type);
                type = type.BaseType;
            } while (type != typeof(Message));
        }

        void DispatchByType(Message message, Type type)
        {
            List<IMessageHandler> list;
            if (!_typeLookup.TryGetValue(type, out list)) return;
            foreach (var handler in list)
            {
                handler.TryHandle(message);
            }
        }
    }
}