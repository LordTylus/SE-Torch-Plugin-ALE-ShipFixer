using NLog;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace ALE_ShipFixer {

    public class Commands : CommandModule {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public ShipFixerPlugin Plugin => (ShipFixerPlugin) Context.Plugin;

        [Command("fixshipmod", "Cuts and pastes a ship you are looking at to try to fix various bugs.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void FixShipMod() {

            IMyPlayer player = Context.Player;

            long playerId = 0L;

            if (player == null) {

                Context.Respond("Console has no Character so cannot use this command. Use !fixshipmod <Gridname> instead!");
                return;

            } else {
                playerId = player.IdentityId;
            }

            if (player.Character == null) {
                Context.Respond("You have no Character currently. Make sure to spawn and be out of cockpit!");
                return;
            }

            if (!checkConformation(playerId, "nogrid"))
                return;

            try {

                Plugin.fixShip(player.Character, 0, Context);

            } catch (Exception e) {
                Log.Error("Error on fixing ship", e);
            }
        }

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

        [Command("fixship", "Cuts and pastes a ship you are looking at to try to fix various bugs.")]
        [Permission(MyPromoteLevel.None)]
        public void FixShip() {

            IMyPlayer player = Context.Player;

            long playerId = 0L;

            if (player == null) {

                Context.Respond("Console has no Grids so cannot use this command. Use !fixshipmod <Gridname> instead!");
                return;

            } else {
                playerId = player.IdentityId;
            }

            if (player.Character == null) {
                Context.Respond("You have no Character currently. Make sure to spawn and be out of cockpit!");
                return;
            }

            var currentCooldownMap = Plugin.CurrentCooldownMap;

            CurrentCooldown currentCooldown = null;

            if (currentCooldownMap.TryGetValue(playerId, out currentCooldown)) {

                long remainingSeconds = currentCooldown.getRemainingSeconds(null);

                if (remainingSeconds > 0) {
                    Log.Info("Cooldown for Player " + player.DisplayName + " still running! " + remainingSeconds + " seconds remaining!");
                    Context.Respond("Command is still on cooldown for " + remainingSeconds + " seconds.");
                    return;
                }

            } else {

                currentCooldown = new CurrentCooldown(Plugin.Cooldown);
                currentCooldownMap.Add(playerId, currentCooldown);
            }

            if (!checkConformation(playerId, "nogrid"))
                return;

            try {

                if(Plugin.fixShip(player.Character, playerId, Context)) {

                    Log.Info("Cooldown for Player " + player + " started!");
                    currentCooldown.startCooldown(null);
                }

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

                Context.Respond("Console has no Grids so cannot use this command. Use !fixshipmod <Gridname> instead!");
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

                if(Plugin.fixShip(gridName, playerId, Context)) {

                    Log.Info("Cooldown for Player " + player + " started!");
                    currentCooldown.startCooldown(null);
                }

            } catch (Exception e) {
                Log.Error("Error on fixing ship", e);
            }
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
