using NLog;
using Sandbox.Game;
using System;
using System.Collections.Generic;
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

            IMyPlayer player = Context.Player;

            long playerId = 0L;

            if (player == null)
                return;
            else
                playerId = player.IdentityId;

            try {

                Plugin.fixShip(gridName, 0, playerId);

            } catch (Exception e) {
                Log.Error("Error on fixing ship", e);
            }
        }

        [Command("fixship", "Cuts and pastes a ship with the given name to try to fix various bugs.")]
        [Permission(MyPromoteLevel.None)]
        public void FixShipPlayer(string gridName) {

            IMyPlayer player = Context.Player;

            long playerId = 0L;

            if (player == null)
                return;
            else
                playerId = player.IdentityId;

            var currentCooldownMap = Plugin.CurrentCooldownMap;

            CurrentCooldown currentCooldown = null;

            Log.Warn(currentCooldownMap.Count);

            if(currentCooldownMap.TryGetValue(playerId, out currentCooldown)) {

                long remainingSeconds = currentCooldown.getRemainingSeconds();

                Log.Warn(remainingSeconds + "");

                if (remainingSeconds > 0) {
                    MyVisualScriptLogicProvider.SendChatMessage("Command is still on cooldown for "+ remainingSeconds + " seconds.", "Server", playerId, "Red");
                    return;
                }

            } else {

                currentCooldown = new CurrentCooldown(playerId);
                currentCooldownMap.Add(playerId, currentCooldown);

                Log.Warn("added");
            }

            try { 

                Plugin.fixShip(gridName, playerId, playerId);

            } catch (Exception e) {
                Log.Error("Error on fixing ship", e);
            }

            Log.Info("Cooldown for Player "+player+" started!");
            currentCooldown.startCooldown();
        }
    }
}
