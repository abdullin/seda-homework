using System;

namespace Koan
{
    public sealed class NodeController : IHandle<Message>
    {
        IPublisher _outputBus;
        QueuedHandler _mainQueue;

        public enum NodeState
        {
            Initializing,
            Starting,
            Started,
            ShuttingDown,
            Shutdown
        }

        readonly FiniteStateMachine<NodeState> _finiteStateMachine; 

        NodeState _state = NodeState.Initializing;

        public NodeController(IPublisher outputBus)
        {
            _outputBus = outputBus;

            _finiteStateMachine = CreateFsm();
        }

        FiniteStateMachine<NodeState> CreateFsm()
        {
            return new FsmBuilder<NodeState>()
                .InAllStates()
                    .When<SystemMessage.SystemInit>().Do(Handle)
                    .When<SystemMessage.BecomeShutDown>().Do(Handle)
                    .When<SystemMessage.ServiceShutdown>().Do(Handle)
                    .When<SystemMessage.StartShutdown>().Do(Handle)
                .WhenOther()
                    .Do(m => _outputBus.Publish(m))
                .Build(() => (int)_state);
        } 

        public void SetMainQueue(QueuedHandler mainQueue)
        {
            _mainQueue = mainQueue;
        }

        void IHandle<Message>.Handle(Message message)
        {
            _finiteStateMachine.Handle(message);
        }

        public void Handle(SystemMessage.SystemInit m)
        {
            Console.WriteLine("Node starting up");
            _outputBus.Publish(m);
        }
        public void Handle(SystemMessage.StartShutdown m)
        {
            Console.WriteLine("going to shutdown");
            _state = NodeState.ShuttingDown;
            _outputBus.Publish(m);
        }
        public void Handle(SystemMessage.BecomeShutDown m)
        {
            Console.WriteLine("Node is down. Bye");
            _state = NodeState.Shutdown;
            _outputBus.Publish(m);
        }

        int _servicesToKill = 1;
        public void Handle(SystemMessage.ServiceShutdown m)
        {
            _servicesToKill -= 1;
            Console.WriteLine("Service {0} reported to be down", m.ServiceName);
            if (_servicesToKill == 0)
            {
                Console.WriteLine("All services down. Good night");
                _outputBus.Publish(new SystemMessage.BecomeShutDown());
            }

        }
    }
}