using System;
using System.Threading;
using RobotNET.API;

namespace RoboChat.Model
{
    class ConnectionThread : IDisposable
    {
        protected const int TIMEOUT = 10000;
        private const int RETRY_TIMEOUT = 10000;

        public static long FlagShouldClose = 0;
        private static long CountCaches = 0;
        protected static long FlagShouldRestart = 0;

        private long FlagIsOnline = 1;
        public event EventHandler<StatusEventArgs> StatusChanged = delegate { };

        private Thread _thread;

        public ConnectionThread()
        {
            _thread = new Thread(new ThreadStart(ThreadProcess));
            _thread.Start();
        }

        public void Dispose()
        {
            _thread.Join();
        }

        private void ThreadProcess()
        {
            while (Interlocked.Read(ref FlagShouldClose) == 0)
            {
                try
                {
                    try
                    {
                        Interlocked.Increment(ref CountCaches);

                        using (var cache = new Cache(Settings.Instance.RobotnetPath, Settings.Instance.CacheName, Settings.Instance.CachePass, TIMEOUT))
                        {
                            if (Interlocked.Exchange(ref FlagIsOnline, 1) == 0)
                            {
                                StatusChanged(this, new StatusEventArgs(true));
                            }

                            ThreadBody(cache);
                        }
                    }
                    finally
                    {
                        if (Interlocked.Exchange(ref FlagIsOnline, 0) == 1)
                        {
                            StatusChanged(this, new StatusEventArgs(false));
                        }

                        Interlocked.Decrement(ref CountCaches);

                        while (Interlocked.Read(ref FlagShouldRestart) != 0)
                            Thread.Sleep(1);
                    }
                }
                catch (Exception ex)
                {
                    // Молча логируем ошибки
                    ex.Process(ErrorHandlingLevels.Silent, GetType().ToString());

                    if (Interlocked.Exchange(ref FlagShouldRestart, 1) == 0)
                    {
                        while (Interlocked.Read(ref CountCaches) != 0)
                            Thread.Sleep(1);

                        Interlocked.Exchange(ref FlagShouldRestart, 0);
                    }
                    else
                    {
                        while (Interlocked.Read(ref FlagShouldRestart) != 0)
                            Thread.Sleep(1);
                    }

                    Thread.Sleep(RETRY_TIMEOUT);
                }
            }
        }

        protected virtual void ThreadBody(Cache cache)
        {
        }
    }
}