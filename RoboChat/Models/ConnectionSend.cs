using System.Collections.Generic;
using System.Threading;
using RoboChat.Contracts;
using RobotNET.API;

namespace RoboChat.Model
{
    class ConnectionSend : ConnectionThread
    {
        private const int SENDING_CHECK_INTERVAL = 50;

        private long CountQueuedMessage = 0;
        private Queue<Message> _queue = new Queue<Message>();

        public void QueueMessage(Message message)
        {
            lock (_queue)
            {
                _queue.Enqueue(message);
                Interlocked.Increment(ref CountQueuedMessage);
            }
        }

        protected override void ThreadBody(Cache cache)
        {
            while (Interlocked.Read(ref FlagShouldClose) == 0 && Interlocked.Read(ref FlagShouldRestart) == 0)
            {
                if (Interlocked.Read(ref CountQueuedMessage) == 0)
                {
                    Thread.Sleep(SENDING_CHECK_INTERVAL);
                    continue;
                }

                Message message = null;

                lock (_queue)
                {
                    Interlocked.Decrement(ref CountQueuedMessage);
                    message = _queue.Dequeue();
                }

                if (message != null)
                {
                    byte[] bytes = Helpers.SerializeToStream(message).ToArray();
                    cache.QueuePending(message.Recipient, bytes, TIMEOUT);
                }
            }

            if (Interlocked.Read(ref FlagShouldClose) == 1)
            {
                cache.QueuePending(Settings.Instance.PublicKey, new byte[] { 1 }, TIMEOUT);
            }
        }
    }
}