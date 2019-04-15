using Torch;

namespace ALE_ShipFixer {
    
    public class ShipFixerConfig : ViewModel
    {
        private int _cooldownInSeconds = 15 * 60; //15 Minutes

        private int _confirmationInSeconds = 30; //30 Seconds

        public int CooldownInSeconds { get => _cooldownInSeconds; set => SetValue(ref _cooldownInSeconds, value); }

        public int ConfirmationInSeconds { get => _confirmationInSeconds; set => SetValue(ref _confirmationInSeconds, value); }
    }
}
