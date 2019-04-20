using System;

namespace RoboChat
{
    public interface IPlugin
    {
        void Run(Uri uri);
    }
}