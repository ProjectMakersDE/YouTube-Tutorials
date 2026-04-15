using System;
using System.Collections.Generic;
using System.Linq;
using PM.Core;
using PM.Enums;
using PM.Interfaces;
using PM.Objects;
using PM.Service;
using UnityEngine;
using UnityEngine.UIElements;

namespace PM.Base
{
    [RequireComponent(typeof(UIDocument))]
    public abstract class BaseUiController<T> : MonoBehaviour, IUiController where T : BaseUiController<T>
    {
        private const int MaxInitializeAttempts = 5;

        public bool IsVisible { get; private set; }

        public UIDocument RootDocument { get; private set; }
        public VisualElement RootVisualElement { get; private set; }

        private readonly List<EventRegistration> _eventRegistrations = new List<EventRegistration>();
        private int _initializeAttempts;

        private void OnEnable()
        {
            ManageEvents("Subscribe");
            App.Events.Subscribe<string>(EventKeys.ShowUi, OnOpenUi);

            _initializeAttempts = 0;
            CancelInvoke(nameof(TryInitializeUi));
            Invoke(nameof(TryInitializeUi), 0f);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(TryInitializeUi));
            ManageEvents("Unsubscribe");
            App.Events.Unsubscribe<string>(EventKeys.ShowUi, OnOpenUi);
        }

        private void TryInitializeUi()
        {
            RootDocument = GetComponent<UIDocument>();
            RootVisualElement = RootDocument?.rootVisualElement;

            if (RootVisualElement == null || RootVisualElement.childCount == 0)
            {
                _initializeAttempts++;

                if (_initializeAttempts < MaxInitializeAttempts)
                    Invoke(nameof(TryInitializeUi), 0.05f);

                return;
            }

            OnAwake();
        }

        private void OnOpenUi(string uiName)
        {
            if (uiName == typeof(T).Name)
                Show(false);
        }

        public virtual void Show(bool force)
        {
            if (!force && IsVisible) return;

            IsVisible = true;

            App.Log.Debug($"{name} - Show");

            RootVisualElement.style.display = DisplayStyle.Flex;
            RootVisualElement.style.visibility = Visibility.Visible;
            RootVisualElement.style.opacity = 1f;

            OnShow();
        }

        public virtual void DelayedHide(bool force, float delay = 0.5f)
        {
            if (!force && !IsVisible) return;

            IsVisible = false;
            App.Log.Debug($"{name} - DelayedHide");

            Invoke(nameof(DelayedHideInternal), delay);
        }

        public virtual void Hide(bool force)
        {
            if (!force && !IsVisible) return;

            IsVisible = false;
            App.Log.Debug($"{name} - Hide");

            RootVisualElement.style.display = DisplayStyle.None;
            RootVisualElement.style.visibility = Visibility.Hidden;
            RootVisualElement.style.opacity = 0f;

            OnHide();
        }

        private void DelayedHideInternal()
        {
            RootVisualElement.style.display = DisplayStyle.None;
            RootVisualElement.style.visibility = Visibility.Hidden;
            RootVisualElement.style.opacity = 0f;

            OnHide();
        }

        protected virtual void OnShow() { }

        protected virtual void OnHide() { }

        protected virtual void OnAwake() { }

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
