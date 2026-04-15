using System.Collections.Generic;
using System.Linq;
using PM.Interfaces;
using PM.Service;
using UnityEngine;

namespace PM.Core
{
    public class App : MonoBehaviour
    {
        public static ConfigurationService Config
        {
            get { return _configurationService ??= new ConfigurationService(); }
            private set => _configurationService = value;
        }

        public static EventService Events
        {
            get { return _eventService ??= new EventService(); }
            private set => _eventService = value;
        }

        public static Log Log
        {
            get { return _log ??= new Log(); }
            private set => _log = value;
        }

        private static EventService _eventService;
        private static ConfigurationService _configurationService;
        private static Log _log;

        [SerializeField] private List<MonoBehaviour> Managers = new List<MonoBehaviour>();

#if UNITY_EDITOR
        [ContextMenu("Load All Managers")]
        private void LoadAllManagers()
        {
            var allManagers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(m => m is IManager)
                .ToList();

            foreach (var manager in allManagers.Where(manager => !Managers.Contains(manager)))
                Managers.Add(manager);
        }
#endif

        private void Awake()
        {
            InitializeServices();
            InitializeManagers();
        }

        private void InitializeServices()
        {
            Config = _configurationService ?? new ConfigurationService();
            Events = _eventService ?? new EventService();
            Log = _log ?? new Log();
        }

        private void InitializeManagers()
        {
            foreach (var manager in Managers)
            {
                if (manager is IManager initManager)
                {
                    if (!initManager.Init())
                        Debug.Log($"Could not initialize {manager.GetType().Name}");
                }
            }
        }
    }
}
