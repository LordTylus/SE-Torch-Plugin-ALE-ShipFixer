using Torch;

namespace ALE_ShipFixer {
    
    public class ShipFixerConfig : ViewModel
    {
        private int _cooldownInSeconds = 15 * 60; //15 Minutes

        private int _confirmationInSeconds = 30; //30 Seconds

        private bool _removeBlueprintsFromProjectors = true;

        private bool _playerCommandEnabled = true;

        public int CooldownInSeconds { get => _cooldownInSeconds; set => SetValue(ref _cooldownInSeconds, value); }

        public int ConfirmationInSeconds { get => _confirmationInSeconds; set => SetValue(ref _confirmationInSeconds, value); }

        public bool RemoveBlueprintsFromProjectors { get => _removeBlueprintsFromProjectors; set => SetValue(ref _removeBlueprintsFromProjectors, value); }

        public bool PlayerCommandEnabled { get => _playerCommandEnabled; set => SetValue(ref _playerCommandEnabled, value); }
    }
}
