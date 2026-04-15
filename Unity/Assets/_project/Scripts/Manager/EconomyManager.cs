using PM.Base;
using PM.Core;
using PM.Enums;
using UnityEngine;

namespace PM.Manager
{
    public class EconomyManager : BaseManager<EconomyManager>
    {
        private const double OrderGoldReward = 25d;
        private const double OrderReputationReward = 1d;
        private const double PromotionCost = 15d;
        private const double PromotionReputationReward = 2d;

        public double Gold { get; private set; }
        public double Reputation { get; private set; }
        public bool IsLoaded { get; private set; }

        private void Awake()
        {
            RegisterEvent<double>(EventKeys.LoadGold, OnLoadGold);
            RegisterEvent<double>(EventKeys.LoadReputation, OnLoadReputation);
            RegisterEvent(EventKeys.LoadingFinish, OnLoadingFinished);
            RegisterEvent(EventKeys.CompleteOrder, CompleteOrder);
            RegisterEvent(EventKeys.RunPromotion, RunPromotion);
            RegisterEvent(EventKeys.ResetProgress, ResetProgress);
        }

        protected override void OnInit()
        {
            Gold = 0d;
            Reputation = 0d;
            IsLoaded = false;
        }

        public void CompleteOrder()
        {
            SetGold(Gold + OrderGoldReward);
            SetReputation(Reputation + OrderReputationReward);

            App.Log.Info($"Order resolved. Gold: {Gold}, Reputation: {Reputation}");
        }

        public void RunPromotion()
        {
            if (Gold < PromotionCost)
            {
                App.Log.Warning($"Not enough gold for promotion. Need {PromotionCost}, have {Gold}.");
                return;
            }

            SetGold(Gold - PromotionCost);
            SetReputation(Reputation + PromotionReputationReward);

            App.Log.Info($"Promotion started. Gold: {Gold}, Reputation: {Reputation}");
        }

        public void ResetProgress()
        {
            SetGold(0d);
            SetReputation(0d);

            App.Log.Info("Economy progress reset.");
        }

        private void SetGold(double value)
        {
            Gold = value;
            App.Events.Notify(EventKeys.ChangeGold, Gold);
        }

        private void SetReputation(double value)
        {
            Reputation = value;
            App.Events.Notify(EventKeys.ChangeReputation, Reputation);
        }

        private void OnLoadGold(double gold)
        {
            Gold = gold;
            App.Events.Notify(EventKeys.ChangeGold, Gold);
            App.Log.Debug($"Gold loaded: {Gold}");
        }

        private void OnLoadReputation(double reputation)
        {
            Reputation = reputation;
            App.Events.Notify(EventKeys.ChangeReputation, Reputation);
            App.Log.Debug($"Reputation loaded: {Reputation}");
        }

        private void OnLoadingFinished()
        {
            IsLoaded = true;

            App.Log.Info("EconomyManager loaded. Publishing current state.");
        }

#if UNITY_EDITOR
        [ContextMenu("Demo/Complete Order")]
        private void DemoCompleteOrder()
        {
            CompleteOrder();
        }

        [ContextMenu("Demo/Run Promotion")]
        private void DemoRunPromotion()
        {
            RunPromotion();
        }

        [ContextMenu("Demo/Reset Progress")]
        private void DemoResetProgress()
        {
            ResetProgress();
        }
#endif
    }
}
