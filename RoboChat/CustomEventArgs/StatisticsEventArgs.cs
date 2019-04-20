using System;

namespace RoboChat.CustomEventArgs
{
    public class StatisticsEventArgs : EventArgs
    {
        public double Lag { get; set; }
        public StatisticsEventArgs(double lag)
        {
            Lag = lag;
        }
    }
}
