using RoboChat.Contracts;
using System;

namespace RoboChat.CustomEventArgs
{
    public class MessageEventArgs : EventArgs
    {
        public Message Message { get; set; }
        public bool Activate { get; set; }
        public MessageEventArgs(Message message, bool activate)
        {
            Message = message;
            Activate = activate;
        }
    }
}
