using System;

namespace Koan
{
    class Program
    {
        static void Main(string[] args)
        {
            var outputBus = new InMemoryBus("OutputBus");
            var controller = new NodeController(outputBus);
            var mainQueue = new QueuedHandler(controller, "Main Queue");
            controller.SetMainQueue(mainQueue);


            // Hello world service
            var hello = new HelloWorldService(mainQueue);
            outputBus.Subscribe<SystemMessage.SystemInit>(hello);
            outputBus.Subscribe<SystemMessage.StartShutdown>(hello);
            outputBus.Subscribe<HelloWorldMessage.Hi>(hello);


            // TIMER
            var timer = new TimerService(new ThreadBasedScheduler(new RealTimeProvider()));
            outputBus.Subscribe<TimerMessage.Schedule>(timer);


            Console.WriteLine("Starting everything. Press enter to initiate shutdown");

            mainQueue.Start();

            mainQueue.Publish(new SystemMessage.SystemInit());
            Console.ReadLine();
            mainQueue.Publish(new SystemMessage.StartShutdown());
            Console.ReadLine();
        }
    }

    

    public static class SystemMessage
    {
        public class SystemInit : Message{}

        public class SystemStart : Message {}

        public class BecomeShutDown : Message {}

        public class StartShutdown : Message {}

        public class ServiceShutdown : Message
        {
            public readonly string ServiceName;

            public ServiceShutdown(string serviceName)
            {
                if (string.IsNullOrWhiteSpace(serviceName))
                    throw new ArgumentOutOfRangeException("serviceName");
                ServiceName = serviceName;
            }
        }
    }

    
}
