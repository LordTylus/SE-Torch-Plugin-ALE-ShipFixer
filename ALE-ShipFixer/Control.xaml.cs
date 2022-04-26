using System.Windows;
using System.Windows.Controls;

namespace ALE_ShipFixer {

    public partial class Control : UserControl {

        private ShipFixerPlugin Plugin { get; }

        public Control() {
            InitializeComponent();
        }

        public Control(ShipFixerPlugin plugin) : this() {
            Plugin = plugin;
            DataContext = plugin.Config;
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e) {
            Plugin.Save();
        }
    }
}
