using PM.Base;
using PM.Core;
using PM.Enums;
using PM.Plugins;

namespace PM.Manager
{
    public class DataManager : BaseManager<DataManager>
    {
        public float LoadingDelay = 0.5f;

        private void Awake()
        {
            RegisterEvent<double>(EventKeys.ChangeGold, gold => PmPrefs.Save(SaveKeys.Gold, gold));
            RegisterEvent<double>(EventKeys.ChangeReputation, reputation => PmPrefs.Save(SaveKeys.Reputation, reputation));

            Invoke(nameof(LoadGameData), LoadingDelay);
        }

        private void LoadGameData()
        {
            App.Events.Notify(EventKeys.LoadGold, PmPrefs.Load<SaveKeys, double>(SaveKeys.Gold));
            App.Events.Notify(EventKeys.LoadReputation, PmPrefs.Load<SaveKeys, double>(SaveKeys.Reputation));

            App.Events.Notify(EventKeys.LoadingFinish, true);
        }
    }
}