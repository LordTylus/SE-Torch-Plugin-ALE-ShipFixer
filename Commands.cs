using NLog;
using System;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace ALE_ShipFixer {
    public class Commands : CommandModule {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public ShipFixerPlugin Plugin => (ShipFixerPlugin) Context.Plugin;

        [Command("fixshipmod", "Cuts and pastes a ship with the given name to try to fix various bugs.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void FixShipMod(string gridName) {

            long playerId = 0L;

            if (Context.Player != null) 
                playerId = Context.Player.IdentityId;

            if (!checkConformation(playerId, gridName))
                return;

            try {

                Plugin.fixShip(gridName, 0, Context);

            } catch (Exception e) {
                Log.Error("Error on fixing ship", e);
            }
        }

        [Command("fixship", "Cuts and pastes a ship with the given name to try to fix various bugs.")]
        [Permission(MyPromoteLevel.None)]
        public void FixShipPlayer(string gridName) {

            IMyPlayer player = Context.Player;

            long playerId = 0L;

            if (player == null) {

                Context.Respond("Console has no Grids so cannot use this command. Use !fixshipmod instead!");
                return;

            } else {
                playerId = player.IdentityId;
            }

            var currentCooldownMap = Plugin.CurrentCooldownMap;

            CurrentCooldown currentCooldown = null;

            if(currentCooldownMap.TryGetValue(playerId, out currentCooldown)) {

                long remainingSeconds = currentCooldown.getRemainingSeconds(null);

                if (remainingSeconds > 0) {
                    Log.Info("Cooldown for Player " + player.DisplayName + " still running! "+ remainingSeconds + " seconds remaining!");
                    Context.Respond("Command is still on cooldown for "+ remainingSeconds + " seconds.");
                    return;
                }

            } else {

                currentCooldown = new CurrentCooldown(Plugin.Cooldown);
                currentCooldownMap.Add(playerId, currentCooldown);
            }

            if (!checkConformation(playerId, gridName))
                return;

            try { 

                Plugin.fixShip(gridName, playerId, Context);

            } catch (Exception e) {
                Log.Error("Error on fixing ship", e);
            }

            Log.Info("Cooldown for Player "+player+" started!");
            currentCooldown.startCooldown(null);
        }

        private bool checkConformation(long playerId, string gridName) {

            var confirmationCooldownMap = Plugin.ConfirmationsMap;
            CurrentCooldown confirmationCooldown = null;

            if (confirmationCooldownMap.TryGetValue(playerId, out confirmationCooldown)) {

                long remainingSeconds = confirmationCooldown.getRemainingSeconds(gridName);

                if (remainingSeconds == 0) {
                    Context.Respond("It is recommended to take a Blueprint of the ship first (ctrl+b). Repeat command within "+Plugin.CooldownConfirmationSeconds+" seconds.");
                    confirmationCooldown.startCooldown(gridName);
                    return false;
                }

            } else {

                confirmationCooldown = new CurrentCooldown(Plugin.CooldownConfirmation);
                confirmationCooldownMap.Add(playerId, confirmationCooldown);

                Context.Respond("It is recommended to take a Blueprint of the ship first (ctrl+b). Repeat command within " + Plugin.CooldownConfirmationSeconds + " seconds.");

                confirmationCooldown.startCooldown(gridName);
                return false;
            }

            confirmationCooldownMap.Remove(playerId);
            return true;
        }
    }
}
