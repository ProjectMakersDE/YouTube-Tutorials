using PM.Base;
using PM.Core;
using PM.Enums;
using PM.Manager;
using UnityEditor.PackageManager;
using UnityEngine.UIElements;

namespace PM.UiController 
{
    public class EconomyHudUiController : BaseUiController<EconomyHudUiController>
    {
        private Label _goldValueLabel;
        private Label _reputationValueLabel;
        private Label _statusLabel;
        private Button _completeOrderButton;
        private Button _promotionButton;
        private Button _resetButton;

        private double _gold;
        private double _reputation;

        private void Awake()
        {
            RegisterEvent<double>(EventKeys.ChangeGold, OnGoldChanged);
            RegisterEvent<double>(EventKeys.ChangeReputation, OnReputationChanged);
        }

        protected override void OnAwake()
        {
            _goldValueLabel = RootVisualElement.Q<Label>("GoldValueLabel");
            _reputationValueLabel = RootVisualElement.Q<Label>("ReputationValueLabel");
            _statusLabel = RootVisualElement.Q<Label>("StatusLabel");
            _completeOrderButton = RootVisualElement.Q<Button>("CompleteOrderButton");
            _promotionButton = RootVisualElement.Q<Button>("PromotionButton");
            _resetButton = RootVisualElement.Q<Button>("ResetButton");

            if (_completeOrderButton != null)
            {
                _completeOrderButton.clicked -= OnCompleteOrderClicked;
                _completeOrderButton.clicked += OnCompleteOrderClicked;
            }

            if (_promotionButton != null)
            {
                _promotionButton.clicked -= OnPromotionClicked;
                _promotionButton.clicked += OnPromotionClicked;
            }

            if (_resetButton != null)
            {
                _resetButton.clicked -= OnResetClicked;
                _resetButton.clicked += OnResetClicked;
            }

            SetStatus("Bereit.");
            RefreshValues();
            Show(true);
        }

        protected override void OnShow()
        {
            RefreshValues();
        }

        private void OnGoldChanged(double gold)
        {
            _gold = gold;
            RefreshValues();
        }

        private void OnReputationChanged(double reputation)
        {
            _reputation = reputation;
            RefreshValues();
        }

        private void OnCompleteOrderClicked()
        {
            App.Events.Notify(EventKeys.CompleteOrder);
            SetStatus("Bestellung abgeschlossen.");
        }

        private void OnPromotionClicked()
        {
            App.Events.Notify(EventKeys.RunPromotion);
            SetStatus("Promotion gestartet.");
        }

        private void OnResetClicked()
        {
            App.Events.Notify(EventKeys.ResetProgress);
            SetStatus("Fortschritt zurückgesetzt.");
        }

        private void RefreshValues()
        {
            if (_goldValueLabel != null)
                _goldValueLabel.text = _gold.ToString("0");

            if (_reputationValueLabel != null)
                _reputationValueLabel.text = _reputation.ToString("0");
        }

        private void SetStatus(string text)
        {
            if (_statusLabel != null)
                _statusLabel.text = text;
        }
    }
}