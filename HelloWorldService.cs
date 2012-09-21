using System;

namespace Koan
{
    sealed class HelloWorldService : 
        
        IHandle<SystemMessage.SystemInit>,
        IHandle<SystemMessage.StartShutdown>,
        IHandle<HelloWorldMessage.Hi>


    {
        readonly IPublisher _mainQueue;
        readonly IEnvelope _envelope ;
        volatile bool _selfDestruct;
        public HelloWorldService(IPublisher mainQueue)
        {
            _mainQueue = mainQueue;
            _envelope = new PublishEnvelope(_mainQueue);
        }

        public void Handle(SystemMessage.SystemInit message)
        {
            Console.WriteLine("Hello world service starting! It sends Hi to itself.");
            _mainQueue.Publish(new HelloWorldMessage.Hi());
        }

        public void Handle(SystemMessage.StartShutdown message)
        {
            _selfDestruct = true;
            _mainQueue.Publish(new SystemMessage.ServiceShutdown("Hello world"));
        }

        public void Handle(HelloWorldMessage.Hi message)
        {
            if (_selfDestruct) return;
            Console.WriteLine("Sending hi back ;)");
            _mainQueue.Publish(TimerMessage.Schedule.Create(TimeSpan.FromSeconds(1), _envelope, new HelloWorldMessage.Hi()));
        }
    }

    public static class HelloWorldMessage
    {
        public class Hi : Message { }
    }
}