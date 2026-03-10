using System;
using System.Collections.Generic;
using PM.Core;
using PM.Enums;
using PM.Objects;

namespace PM.Service
{
    public class EventService
    {
        private readonly Dictionary<EventKeys, EventData> Events = new Dictionary<EventKeys, EventData>();

        public void Subscribe<T>(EventKeys key, Action<T> action)
        {
            App.Log.Debug($"Subscribe: {key} {action.Method.Name}");

            if (!Events.TryGetValue(key, out var eventData))
            {
                App.Log.Debug($"Event not found: {key}");
                eventData = new EventData();
                Events[key] = eventData;
            }

            eventData.Delegate = Delegate.Combine(eventData.Delegate, action);

            if (eventData.DataReference?.Target is T lastData)
                action(lastData);
        }

        public void Unsubscribe<T>(EventKeys key, Action<T> action)
        {
            App.Log.Debug($"Unsubscribe: {key} {action.Method.Name}");

            if (Events.TryGetValue(key, out var eventData))
                eventData.Delegate = Delegate.Remove(eventData.Delegate, action);
        }

        public void Notify<T>(EventKeys key, T args)
        {
            if (args == null)
            {
                App.Log.Warning($"Notify: {key} - no Data!");
                return;
            }

            App.Log.Debug($"Notify: {key} {args.GetType()}");

            if (!Events.TryGetValue(key, out var eventData))
            {
                App.Log.Debug($"Event not found: {key}");
                eventData = new EventData();
                Events[key] = eventData;
            }

            eventData.DataReference = new WeakReference(args);

            if (eventData.Delegate is Action<T> action)
                action(args);
        }

        public void Subscribe(EventKeys key, Action action)
        {
            App.Log.Debug($"Subscribe (parameterless): {key} {action.Method.Name}");

            if (!Events.TryGetValue(key, out var eventData))
            {
                App.Log.Debug($"Event not found: {key}");
                eventData = new EventData();
                Events[key] = eventData;
            }

            eventData.Delegate = Delegate.Combine(eventData.Delegate, action);
        }

        public void Unsubscribe(EventKeys key, Action action)
        {
            App.Log.Debug($"Unsubscribe (parameterless): {key} {action.Method.Name}");

            if (Events.TryGetValue(key, out var eventData))
                eventData.Delegate = Delegate.Remove(eventData.Delegate, action);
        }

        public void Notify(EventKeys key)
        {
            App.Log.Debug($"Notify (parameterless): {key}");

            if (!Events.TryGetValue(key, out var eventData))
            {
                App.Log.Debug($"Event not found: {key}");
                eventData = new EventData();
                Events[key] = eventData;
            }

            if (eventData.Delegate is Action action)
                action();
        }
    }
}