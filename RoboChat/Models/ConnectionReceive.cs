using RoboChat.Contracts;
using RoboChat.CustomEventArgs;
using RobotNET.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace RoboChat.Model
{
    class ConnectionReceive : ConnectionThread
    {
        private DateTime _lastStatusCheck;

        public event EventHandler<ContactsUpdatedEventArgs> ContactsUpdated = delegate { };
        public event EventHandler<StatisticsEventArgs> StatisticsUpdated = delegate { };
        public event EventHandler<MessageEventArgs> MessageReceived = delegate { };

        protected override void ThreadBody(Cache cache)
        {
            while (Interlocked.Read(ref FlagShouldClose) == 0 && Interlocked.Read(ref FlagShouldRestart) == 0)
            {
                Status(cache);

                // Ждем новое сообщение
                var message = Receive(cache);
                if (message != null)
                {
                    MessageReceived?.Invoke(this, new MessageEventArgs(message, true));
                }
            }
        }

        private Message Receive(Cache cache)
        {
            var bytes = cache.DequeuePending(Settings.Instance.PublicKey, Settings.Instance.PendingInterval);
            if (bytes == null ||
                // Системное сообщение для досрочного завершения
                // Проверяю размер, потому что на данный момент в значении может быть возвращен "хлам"
                bytes.Length == 1)
            {
                return null;
            }

            using (var ms = new MemoryStream(bytes))
                return (Message)Helpers.DeserializeFromStream(ms);
        }

        private void Status(Cache cache)
        {
            if ((DateTime.UtcNow - _lastStatusCheck).TotalMilliseconds < Settings.Instance.StatusInterval)
                return;

            _lastStatusCheck = DateTime.UtcNow;

            // Сообщим всем наши данные
            var contactSelf = new Contact()
            {
                ID = Settings.Instance.PublicKey,
                Name = Settings.Instance.Name,
                Version = Settings.Version,
                LastOnline = _lastStatusCheck,
                RequireEncryption = Settings.Instance.RequireEncryption
            };
            byte[] profileBytes = Helpers.SerializeToStream(contactSelf).ToArray();
            DateTime start = DateTime.Now;
            cache.Set("#PROFILE#" + contactSelf.ID, profileBytes, Settings.Instance.LifetimeInterval, TIMEOUT);
            double lag = (DateTime.Now - start).TotalMilliseconds;

            // Соберем профили всех пользователей онлайн
            var contacts = new List<Contact>();
            var profiles = cache.BatchGet("#PROFILE#", TIMEOUT);
            if (profiles != null)
            {
                foreach (var profile in profiles)
                {
                    if (profile.Value == null)
                        continue;

                    using (var ms = new MemoryStream(profile.Value))
                        contacts.Add((Contact)Helpers.DeserializeFromStream(ms));
                }
            }

            ContactsUpdated?.Invoke(this, new ContactsUpdatedEventArgs(contacts.ToArray()));
            StatisticsUpdated?.Invoke(this, new StatisticsEventArgs(lag));
        }
    }
}