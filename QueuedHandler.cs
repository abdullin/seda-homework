using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace Koan
{
    public sealed class QueuedHandler : IHandle<Message>, IPublisher
    {
        readonly IHandle<Message> _consumer;
        readonly ConcurrentQueue<Message> _queue = new ConcurrentQueue<Message>();

        Task _thread;

        readonly int _waitToStopThreadMs;

        readonly string _name;
        readonly CancellationTokenSource _cancel = new CancellationTokenSource();


        public QueuedHandler(IHandle<Message> consumer, string name, int waitToStopThreadMs = 10000)
        {
            _consumer = consumer;
            _name = name;
            _waitToStopThreadMs = waitToStopThreadMs;
        }

        public void Start()
        {
            if (null != _thread)
                throw new InvalidOperationException("Thread is already running");


            _thread = Task.Factory.StartNew(() => ReadMessagesFromQueue(_cancel.Token), TaskCreationOptions.LongRunning);
            
        }

        public void Stop()
        {
            _cancel.Cancel();
            if (null == _thread) return;
            if (_thread.Wait(_waitToStopThreadMs))
            {
                throw new InvalidOperationException("Failed to stop thread ");
            }
            
        }

        public void Handle(Message message)
        {
            Contract.Requires(message != null);
            _queue.Enqueue(message);
        }

        void ReadMessagesFromQueue(CancellationToken token)
        {
            Thread.CurrentThread.Name = _name;

            while (!token.IsCancellationRequested)
            {
                Message result;
                
                if (_queue.TryDequeue(out result))
                {
                    try
                    {
                        _consumer.Handle(result); 
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }
                else
                {
                    token.WaitHandle.WaitOne(1);
                }
            }
        }

        public void Publish(Message message)
        {
            Contract.Requires(message != null);
            _queue.Enqueue(message);
        }
    }
}