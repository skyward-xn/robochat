using RoboChat.Enums;
using System;
using System.IO;

namespace RoboChat.Contracts
{
    [Serializable]
    public class Message
    {
        public string Text { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public DateTime Sent { get; set; }
        public DateTime Received { get; set; }
        public bool IsNotification { get; set; }
        public bool IsFile { get; set; }
        public string FilePath { get; set; }
        public long FileOffset { get; set; }
        public long FileLength { get; set; }
        public bool IsEncrypted { get; set; }
        public string Raw { get; set; }

        public Message()
        {
        }

        public Message(string text, string from, string to, DateTime? sent = null, DateTime? received = null)
        {
            Text = text;
            From = from;
            To = to;
            Sent = sent.HasValue ? sent.Value : DateTime.Now;
            Received = received.HasValue ? received.Value : DateTime.Now;
        }

        public Message(Message message)
        {
            Text = message.Text;
            From = message.From;
            To = message.To;
            Sent = message.Sent;
            Received = message.Received;
            IsNotification = message.IsNotification;
            IsFile = message.IsFile;
            FilePath = message.FilePath;
            FileOffset = message.FileOffset;
            FileLength = message.FileLength;
            IsEncrypted = message.IsEncrypted;
            Raw = message.Raw;
        }

        public Directions Direction
        {
            get
            {
                // From == Settings.Name добавлено для обратной совместимости
                return From == Settings.Instance.PublicKey || From == Settings.Instance.Name ? Directions.Out : Directions.In;
            }
        }

        public string Recipient
        {
            get
            {
                return IsNotification ? From : To;
            }
        }

        public string Address
        {
            get
            {
                return Direction == Directions.Out ? To : From;
            }
        }

        public string TooltipText
        {
            get
            {
                if (Received == DateTime.MinValue)
                    return string.Format("Sent: {0}", Sent);

                return string.Format("Sent: {0}{2}Received: {1}", Sent, Received, Environment.NewLine);
            }
        }

        public string ID
        {
            get
            {
                return string.Format("{0}->{1}:{2}", From, To, Sent.ToBinary());
            }
        }

        public string FileName
        {
            get
            {
                return Path.GetFileName(FilePath);
            }
        }
    }
}
