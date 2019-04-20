using System;
using System.Windows;

namespace RoboChat
{
    public enum ErrorHandlingLevels
    {
        Silent, Tray, Modal
    }

    public enum Directions
    {
        In, Out
    }

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

    public class StatusEventArgs : EventArgs
    {
        public bool IsOnline { get; set; }
        public StatusEventArgs(bool isOnline)
        {
            IsOnline = isOnline;
        }
    }

    public class ContactsUpdatedEventArgs : EventArgs
    {
        public Contact[] Contacts { get; set; }
        public ContactsUpdatedEventArgs(Contact[] contacts)
        {
            Contacts = contacts;
        }
    }

    public class StatisticsEventArgs : EventArgs
    {
        public double Lag { get; set; }
        public StatisticsEventArgs(double lag)
        {
            Lag = lag;
        }
    }

    public class WindowSettings
    {
        public string Name;
        public Rect Rect;
    }

    public enum SendKeyTypes
    {
        Enter, CtrlEnter
    }

    public enum Tags
    {
        Bold, Italic, Underline, Image, Smile, Url, Newline, Color, Quote
    };

    public class UpdaterState
    {
        public bool IsDialog;
    }
}