using ALE_Core;
using ALE_Core.Cooldown;
using ALE_Core.Utils;
using NLog;
using Sandbox.Game.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using VRage.Groups;

namespace ALE_ShipFixer {

    public class Commands : CommandModule {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public ShipFixerPlugin Plugin => (ShipFixerPlugin) Context.Plugin;

        [Command("fixshipmod", "Cuts and pastes a ship with the given name to try to fix various bugs.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void FixShipMod() {

            List<string> args = Context.Args;

            if (args.Count == 0) {

                FixShipModLookedAt();

            } else {

                if (args.Count != 1)
                    Context.Respond("Correct Usage is !fixshipmod <gridName>");

                FixShipModGridName(args[0]);
            }
        }

        public void FixShipModGridName(string gridName) {

            var steamId = new SteamIdCooldownKey(PlayerUtils.GetSteamId(Context.Player));

            if (!CheckConformation(steamId, 0, gridName, null))
                return;

            try {

                var result = Plugin.FixShip(gridName, 0);
                WriteResponse(result);

            } catch (Exception e) {
                Log.Error(e, "Error on fixing ship");
            }
        }

        public void FixShipModLookedAt() {

            if (Context.Player == null) {

                Context.Respond("Console has no Character so cannot use this command. Use !fixshipmod <Gridname> instead!");
                return;
            } 

            IMyCharacter character = Context.Player.Character;

            if (character == null) {
                Context.Respond("You have no Character currently. Make sure to spawn and be out of cockpit!");
                return;
            }

            var steamId = new SteamIdCooldownKey(PlayerUtils.GetSteamId(Context.Player));

            if (!CheckConformation(steamId, 0, "nogrid", character))
                return;

            try {

                var result = Plugin.FixShip(character, 0);
                WriteResponse(result);

            } catch (Exception e) {
                Log.Error(e, "Error on fixing ship");
            }
        }

        [Command("fixship", "Cuts and pastes a ship you are looking at or with the given name to try to fix various bugs.")]
        [Permission(MyPromoteLevel.None)]
        public void FixShipPlayer() {

            if(!Plugin.PlayerCommandEnabled) {
                Context.Respond("This command was disabled for players use!");
                return;
            }

            List<string> args = Context.Args;

            if (args.Count == 0) {

                FixShipPlayerLookAt();

            } else {

                if (args.Count != 1)
                    Context.Respond("Correct Usage is !fixship <gridName>");

                FixShipPlayerGridName(args[0]);
            }
        }

        public void FixShipPlayerGridName(string gridName) {

            IMyPlayer player = Context.Player;

            long playerId;

            if (player == null) {

                Context.Respond("Console has no Grids so cannot use this command. Use !fixshipmod <Gridname> instead!");
                return;

            } else {
                playerId = player.IdentityId;
            }

            CooldownManager cooldownManager = Plugin.CommandCooldownManager;

            var steamId = new SteamIdCooldownKey(PlayerUtils.GetSteamId(Context.Player));

            if(!cooldownManager.CheckCooldown(steamId, null, out long remainingSeconds)) {
                Log.Info("Cooldown for Player " + player.DisplayName + " still running! " + remainingSeconds + " seconds remaining!");
                Context.Respond("Command is still on cooldown for " + remainingSeconds + " seconds.");
                return;
            }

            if (!CheckConformation(steamId, playerId, gridName, null))
                return;

            try {

                var result = Plugin.FixShip(gridName, playerId);
                WriteResponse(result);

                if (result == CheckResult.SHIP_FIXED) {

                    Log.Info("Cooldown for Player " + player.DisplayName + " started!");
                    cooldownManager.StartCooldown(steamId, null, Plugin.Cooldown);
                }

            } catch (Exception e) {
                Log.Error(e, "Error on fixing ship");
            }
        }

        public void FixShipPlayerLookAt() {

            IMyPlayer player = Context.Player;

            long playerId;

            if (player == null) {

                Context.Respond("Console has no Grids so cannot use this command. Use !fixshipmod <Gridname> instead!");
                return;

            } else {
                playerId = player.IdentityId;
            }

            IMyCharacter character = player.Character;

            if (character == null) {
                Context.Respond("You have no Character currently. Make sure to spawn and be out of cockpit!");
                return;
            }

            CooldownManager cooldownManager = Plugin.CommandCooldownManager;

            var steamId = new SteamIdCooldownKey(PlayerUtils.GetSteamId(Context.Player));

            if (!cooldownManager.CheckCooldown(steamId, null, out long remainingSeconds)) {
                Log.Info("Cooldown for Player " + player.DisplayName + " still running! " + remainingSeconds + " seconds remaining!");
                Context.Respond("Command is still on cooldown for " + remainingSeconds + " seconds.");
                return;
            }

            if (!CheckConformation(steamId, playerId, "nogrid", character))
                return;

            try {

                var result = Plugin.FixShip(character, playerId);
                WriteResponse(result);

                if (result == CheckResult.SHIP_FIXED) {

                    Log.Info("Cooldown for Player " + player.DisplayName + " started!");
                    cooldownManager.StartCooldown(steamId, null, Plugin.Cooldown);
                }

            } catch (Exception e) {
                Log.Error(e, "Error on fixing ship");
            }
        }

        private bool CheckConformation(ICooldownKey cooldownKey, long playerId, string gridName, IMyCharacter character) {

            var cooldownManager = Plugin.ConfirmationCooldownManager;
            if (!cooldownManager.CheckCooldown(cooldownKey, gridName, out _)) {
                cooldownManager.StopCooldown(cooldownKey);
                return true;
            }

            if (!CheckGridFound(playerId, gridName, character, out MyCubeGrid grid))
                return false;

            if(grid != null)
                Context.Respond("Are you sure you want to continue? Enter the command again within " + Plugin.CooldownConfirmationSeconds + " seconds to confirm fixship on " + grid.DisplayName + ".");
            else
                Context.Respond("Are you sure you want to continue? Enter the command again within " + Plugin.CooldownConfirmationSeconds + " seconds.");

            cooldownManager.StartCooldown(cooldownKey, gridName, Plugin.Cooldown);

            return false;
        }

        private bool CheckGridFound(long playerId, string gridName, IMyCharacter character, out MyCubeGrid grid) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups;

            if (character == null)
                groups = ShipFixerPlugin.FindGridGroupsForPlayer(gridName, playerId, Plugin.FactionFixEnabled);
            else
                groups = ShipFixerPlugin.FindLookAtGridGroup(character, playerId, Plugin.FactionFixEnabled);

            CheckResult result = ShipFixerPlugin.CheckGroups(groups, out _, playerId, Plugin.FactionFixEnabled);

            grid = null;

            if (result != CheckResult.OK) {
                
                WriteResponse(result);

                return false;
            }

            List<MyCubeGrid> grids = new List<MyCubeGrid>();

            foreach(var group in groups) {

                foreach (var node in group.Nodes)
                    grids.Add(node.NodeData);

                break;
            }

            grid = GridUtils.GetBiggestGridInGroup(grids);

            return true;
        }

        private void WriteResponse(CheckResult result) {

            switch(result) {

                case CheckResult.TOO_FEW_GRIDS:
                    Context.Respond("Could not find your Grid. Check if ownership is correct");
                    break;

                case CheckResult.TOO_MANY_GRIDS:
                    Context.Respond("Found multiple Grids with same Name. Rename your grid first to something unique.");
                    break;

                case CheckResult.UNKNOWN_PROBLEM:
                    Context.Respond("Could not work with found grid for unknown reason.");
                    break;

                case CheckResult.OWNED_BY_DIFFERENT_PLAYER:
                    Context.Respond("Grid seems to be owned by a different player.");
                    break;

                case CheckResult.DIFFERENT_OWNER_ON_CONNECTED_GRID:
                    Context.Respond("One of the connected grids is owned by a different player.");
                    break;

                case CheckResult.GRID_OCCUPIED:
                    Context.Respond("Cockpits or seats are still occupied! Clear them first and try again. Dont forget to check the toilet!");
                    break;

                case CheckResult.SHIP_FIXED:
                    Context.Respond("Ship was fixed!");
                    break;
            }
        }
    }
}
