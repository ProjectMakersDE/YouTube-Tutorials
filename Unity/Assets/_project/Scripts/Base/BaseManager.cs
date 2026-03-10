using System;
using System.Collections.Generic;
using System.Linq;
using PM.Core;
using PM.Enums;
using PM.Interfaces;
using PM.Objects;
using PM.Service;
using UnityEngine;

namespace PM.Base
{
    public abstract class BaseManager<T> : MonoBehaviour, IManager where T : BaseManager<T>
    {
        public static T Instance;

        private readonly List<EventRegistration> _eventRegistrations = new List<EventRegistration>();

        public virtual bool Init()
        {
            try
            {
                if (Instance != null && Instance != this)
                {
                    Destroy(this.gameObject);
                    return true;
                }

                Instance = (T)this;
                OnInit();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not initialize {GetType().Name}: {e.Message}");
                return false;
            }

            return true;
        }

        protected virtual void OnInit() { }

        protected virtual void OnEnable()
        {
            ManageEvents("Subscribe");
        }

        protected virtual void OnDisable()
        {
            ManageEvents("Unsubscribe");
        }

        protected void RegisterEvent(EventKeys eventKey, Action handler)
        {
            _eventRegistrations.Add(new EventRegistration
            {
                EventKey = eventKey,
                EventType = null,
                Handler = handler
            });
        }

        protected void RegisterEvent<TR>(EventKeys eventKey, Action<TR> handler)
        {
            _eventRegistrations.Add(new EventRegistration
            {
                EventKey = eventKey,
                EventType = typeof(TR),
                Handler = handler
            });
        }

        private void ManageEvents(string methodName)
        {
            foreach (var registration in _eventRegistrations)
            {
                switch (methodName)
                {
                    case "Subscribe" when registration.EventType == null:
                    {
                        var subscribeMethod = typeof(EventService).GetMethod("Subscribe", new[] { typeof(EventKeys), typeof(Action) });
                        subscribeMethod?.Invoke(App.Events, new object[] { registration.EventKey, registration.Handler });
                        break;
                    }
                    case "Subscribe":
                    {
                        var subscribeMethods = typeof(EventService).GetMethods().Where(m => m.Name == "Subscribe" && m.IsGenericMethodDefinition && m.GetParameters().Length == 2);
                        var subscribeMethod = subscribeMethods.FirstOrDefault();
                        var genericMethod = subscribeMethod?.MakeGenericMethod(registration.EventType);
                        genericMethod?.Invoke(App.Events, new object[] { registration.EventKey, registration.Handler });
                        break;
                    }
                    case "Unsubscribe" when registration.EventType == null:
                    {
                        var unsubscribeMethod = typeof(EventService).GetMethod("Unsubscribe", new[] { typeof(EventKeys), typeof(Action) });
                        unsubscribeMethod?.Invoke(App.Events, new object[] { registration.EventKey, registration.Handler });
                        break;
                    }
                    case "Unsubscribe":
                    {
                        var unsubscribeMethods = typeof(EventService).GetMethods().Where(m => m.Name == "Unsubscribe" && m.IsGenericMethodDefinition && m.GetParameters().Length == 2);
                        var unsubscribeMethod = unsubscribeMethods.FirstOrDefault();
                        var genericMethod = unsubscribeMethod?.MakeGenericMethod(registration.EventType);
                        genericMethod?.Invoke(App.Events, new object[] { registration.EventKey, registration.Handler });
                        break;
                    }
                }
            }
        }
    }
}