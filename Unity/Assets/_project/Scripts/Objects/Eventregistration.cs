using System;
using PM.Enums;

namespace PM.Objects
{
    public struct EventRegistration
    {
        public EventKeys EventKey;
        public Type EventType;
        public Delegate Handler;
    }
}