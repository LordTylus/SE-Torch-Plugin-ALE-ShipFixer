using NLog;
using Torch;
using Torch.API;
using System.IO;
using System;
using Torch.API.Plugins;
using System.Windows.Controls;
using ALE_Core.Cooldown;

namespace ALE_ShipFixer {
    public class ShipFixerPlugin : TorchPluginBase, IWpfPlugin {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static ShipFixerPlugin Instance;
        private Control _control;
        public UserControl GetControl() => _control ?? (_control = new Control(this));
        private Persistent<ShipFixerConfig> _config;
        public ShipFixerConfig Config => _config?.Data;
        public CooldownManager CommandCooldownManager { get; } = new CooldownManager();
        public CooldownManager ConfirmationCooldownManager { get; } = new CooldownManager();

        public long Cooldown { get { return Config.CooldownInSeconds * 1000; } }
        public long CooldownConfirmationSeconds { get { return Config.ConfirmationInSeconds; } }
        public long CooldownConfirmation { get { return Config.ConfirmationInSeconds * 1000; } }
        public bool PlayerCommandEnabled { get { return Config.PlayerCommandEnabled; } }
        public bool FactionFixEnabled { get { return Config.FixShipFactionEnabled; } }

        /// <inheritdoc />
        public override void Init(ITorchBase torch) {

            base.Init(torch);
            Instance = this;

            var configFile = Path.Combine(StoragePath, "ShipFixer.cfg");

            try {
                _config = Persistent<ShipFixerConfig>.Load(configFile);
            } catch (Exception e) {
                Log.Warn(e);
            }

            if (_config?.Data == null) {
                Log.Info("Create Default Config, because none was found!");

                _config = new Persistent<ShipFixerConfig>(configFile, new ShipFixerConfig());
                Save();
            }

            ShipFixerCore.Init();
        }

        public void Save() {

            try {

                _config.Save();

                Log.Info("Configuration Saved.");

            } catch (IOException) {
                Log.Warn("Configuration failed to save");
            }
        }
    }
}