using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace PM.Service
{
    public class ConfigurationService
    {
        private const string ConfigFileFolder = "Config";
        private const string ConfigFileName = "Config.ini";
        
        private Dictionary<string, NameValueCollection> _sections;
        private readonly Dictionary<string, object> _cache = new Dictionary<string, object>();

        public ConfigurationService()
        {
            OnInit();
        }

        private void OnInit()
        {
            var path = Path.Combine(Application.streamingAssetsPath, ConfigFileFolder, ConfigFileName);
            Debug.Log($"Loading config file from {path}");

            UniTask.FromResult(LoadFileContent(path));
        }

        public T GetValue<T>(string section, string key, T defaultValue = default)
        {
            string cacheKey = $"{section}:{key}";

            if (_cache.TryGetValue(cacheKey, out object cachedValue) && cachedValue is T typedValue)
                return typedValue;

            if (_sections == null || !_sections.TryGetValue(section, out NameValueCollection sectionValues) || sectionValues[key] == null)
                return defaultValue;

            try
            {
                T value = (T)Convert.ChangeType(sectionValues[key], typeof(T));
                _cache[cacheKey] = value;
                return value;
            }
            catch
            {
                Debug.LogWarning($"Failed to convert value for {section}:{key} to type {typeof(T)}");
                return defaultValue;
            }
        }

        public List<string> GetSections()
        {
            if (_sections == null)
            {
                Debug.LogError("Configuration not loaded yet.");
                return new List<string>();
            }

            return new List<string>(_sections.Keys);
        }

        public NameValueCollection GetSectionValues(string section)
        {
            if (_sections == null || !_sections.TryGetValue(section, out NameValueCollection sectionValues))
            {
                Debug.LogWarning($"Section '{section}' not found in configuration.");
                return null;
            }

            return sectionValues;
        }
        
        private async UniTask LoadFileContent(string filePath)
        {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_IOS
    filePath = "file://" + filePath;
#endif

            await UniTask.SwitchToMainThread();
            using UnityWebRequest request = UnityWebRequest.Get(filePath);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to load file: {request.error}");
                return;
            }

            ParseConfigContent(request.downloadHandler.text);
        }

        private void ParseConfigContent(string content)
        {
            _sections = new Dictionary<string, NameValueCollection>();
            string currentSection = "";

            using StringReader reader = new StringReader(content);
            while (reader.ReadLine() is { } line)
            {
                string trimmedLine = line.Trim();

                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#'))
                    continue;

                if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    _sections[currentSection] = new NameValueCollection();
                }
                else if (trimmedLine.Contains("="))
                {
                    if (string.IsNullOrEmpty(currentSection))
                    {
                        Debug.LogWarning("Key-value pair found outside of a section. Ignoring line: " + line);
                        continue;
                    }

                    string[] keyValue = trimmedLine.Split(new[] { '=' }, 2);
                    _sections[currentSection].Add(keyValue[0].Trim(), keyValue[1].Trim());
                }
            }

            Debug.Log($"Config file parsed with {_sections.Count} sections");
        }
    }
}