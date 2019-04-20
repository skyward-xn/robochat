using RoboChat.Contracts;
using System;

namespace RoboChat.CustomEventArgs
{
    public class ContactsUpdatedEventArgs : EventArgs
    {
        public Contact[] Contacts { get; set; }
        public ContactsUpdatedEventArgs(Contact[] contacts)
        {
            Contacts = contacts;
        }
    }
}
