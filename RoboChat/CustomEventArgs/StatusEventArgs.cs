using System;

namespace RoboChat.CustomEventArgs
{
    public class StatusEventArgs : EventArgs
    {
        public bool IsOnline { get; set; }
        public StatusEventArgs(bool isOnline)
        {
            IsOnline = isOnline;
        }
    }
}
