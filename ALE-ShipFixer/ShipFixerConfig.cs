using Torch;

namespace ALE_ShipFixer {
    public class ShipFixerConfig : ViewModel {

        private int _cooldownInSeconds = 5 * 60; //5 Minutes
        private int _confirmationInSeconds = 30; //30 Seconds
        private bool _removeBlueprintsFromProjectors = false;
        private bool _playerCommandEnabled = true;
        private bool _fixShipFactionEnabled = false;

        public int CooldownInSeconds { get => _cooldownInSeconds; set => SetValue(ref _cooldownInSeconds, value); }

        public int ConfirmationInSeconds { get => _confirmationInSeconds; set => SetValue(ref _confirmationInSeconds, value); }

        public bool RemoveBlueprintsFromProjectors { get => _removeBlueprintsFromProjectors; set => SetValue(ref _removeBlueprintsFromProjectors, value); }

        public bool PlayerCommandEnabled { get => _playerCommandEnabled; set => SetValue(ref _playerCommandEnabled, value); }

        public bool FixShipFactionEnabled { get => _fixShipFactionEnabled; set => SetValue(ref _fixShipFactionEnabled, value); }
    }
}