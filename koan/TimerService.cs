using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Koan
{
    public sealed class TimerService : 
        IHandle<TimerMessage.Schedule>, IDisposable
    {
        readonly IScheduler _scheduler;
        public TimerService(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public void Handle(TimerMessage.Schedule message)
        {
            _scheduler.Schedule(message.TriggerAfter, CallBack, message);
        }

        static void CallBack(IScheduler scheduler, object o)
        {
            var msg = (TimerMessage.Schedule) o;
            msg.Reply();
        }


        public void Dispose()
        {
            _scheduler.Dispose();
        }
    }

    public static class TimerMessage
    {
        public class Schedule : Message
        {
            public readonly TimeSpan TriggerAfter;

            public readonly Action Reply;

            public Schedule(TimeSpan triggerAfter, Action reply)
            {
                TriggerAfter = triggerAfter;
                Reply = reply;
            }

            public static Schedule Create<T>(TimeSpan trigerAfter, IEnvelope reply, T replyMessage) where T : Message
            {
                return new Schedule(trigerAfter, () => reply.ReplyWith(replyMessage));
            }

        }
    }


    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
    }
    public sealed class RealTimeProvider : ITimeProvider
    {
        public DateTime UtcNow { get { return DateTime.UtcNow; } }
    }

    public interface IScheduler : IDisposable
    {

        void Schedule(TimeSpan fireIn, Action<IScheduler, object> action, object state);

    }

    public sealed class ThreadBasedScheduler : IScheduler
    {
        readonly ITimeProvider _provider;
        volatile bool _selfDestruct;
        readonly Thread _thread;

        readonly ConcurrentQueue<ScheduledTask> _toSchedule = new ConcurrentQueue<ScheduledTask>();
        // can replace this with Paired heap from Event Store
        readonly SortedList<DateTime, ScheduledTask> _tasks = new SortedList<DateTime, ScheduledTask>();
        public ThreadBasedScheduler(ITimeProvider provider)
        {
            _provider = provider;
            _thread = new Thread(Start)
            {
                IsBackground = true,
                Name = "Scheduler"
            };
            _thread.Start();
        }

        void Start()
        {
            while (!_selfDestruct)
            {
                ScheduledTask task;
                while (_toSchedule.TryDequeue(out task))
                {
                    _tasks.Add(task.DueTimeUtc, task);
                }
                bool processed = false;
                while (_tasks.Count > 0 && _tasks.First().Key <= _provider.UtcNow)
                {
                    var scheduled = _tasks.First();
                    _tasks.RemoveAt(0);
                    scheduled.Value.Action(this, scheduled.Value.State);
                    processed = true;
                }

                if (!processed)
                    Thread.Sleep(1);
            }
        }


        public void Schedule(TimeSpan fireIn, Action<IScheduler, object> action, object state)
        {
            _toSchedule.Enqueue(new ScheduledTask(_provider.UtcNow.Add(fireIn), action, state));
        }

        public void Stop()
        {
            _selfDestruct = true;
        }


        sealed class ScheduledTask
        {
            public readonly DateTime DueTimeUtc;
            public readonly Action<IScheduler, object> Action;
            public readonly object State;

            public ScheduledTask(DateTime dueTimeUtc, Action<IScheduler, object> action, object state)
            {
                DueTimeUtc = dueTimeUtc;
                Action = action;
                State = state;
            }
        }

        public void Dispose()
        {
            _selfDestruct = true;
        }
    }
}