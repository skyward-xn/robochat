using RoboChat.Contracts;
using System;

namespace RoboChat.Interfaces
{
    public interface IHistoryStorage
    {
        void OpenFolderByName(string name);
        string AddFileByName(string name, string filepath);
        void Add(string contactNameForHistory, string contactNameForAuthor, Message message);
        Message[] GetByNameAndDate(string name, DateTime dateTime);
        void ClearByName(string name);
    }
}
