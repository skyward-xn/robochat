using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Documents;

namespace RoboChat
{
    public interface IRosterModel : IDisposable
    {
        void Start();
        ObservableCollection<Contact> Contacts { get; }
        event EventHandler<MessageEventArgs> MessageReceived;
        event EventHandler<StatusEventArgs> StatusChanged;
        event EventHandler<StatisticsEventArgs> StatisticsUpdated;
        Message Send(string toAddress, string text);
        void SendFile(Message message);
        Message[] GetFromHistory(string address, DateTime dateTime);
        void OpenHistory(string address);
        void ClearHistory(string address);
        string GetTag(Tags tag, string param = "", string attr = "");
        string GetTag(string tagName, string param = "", string attr = "");
        void ProcessMessage(Span span, string messageText, ref List<Image> images);
    }
}