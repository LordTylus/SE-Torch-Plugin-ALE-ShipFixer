using ALE_Core;
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

            long playerId = 0L;

            if (Context.Player != null)
                playerId = Context.Player.IdentityId;

            if (!CheckConformation(playerId, 0, gridName, null))
                return;

            try {

                Plugin.FixShip(gridName, 0, Context);

            } catch (Exception e) {
                Log.Error(e, "Error on fixing ship");
            }
        }

        public void FixShipModLookedAt() {

            IMyPlayer player = Context.Player;

            long playerId;

            if (player == null) {

                Context.Respond("Console has no Character so cannot use this command. Use !fixshipmod <Gridname> instead!");
                return;

            } else {
                playerId = player.IdentityId;
            }

            IMyCharacter character = player.Character;

            if (character == null) {
                Context.Respond("You have no Character currently. Make sure to spawn and be out of cockpit!");
                return;
            }

            if (!CheckConformation(playerId, 0, "nogrid", character))
                return;

            try {

                Plugin.FixShip(character, 0, Context);

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

            var currentCooldownMap = Plugin.CurrentCooldownMap;

            if (currentCooldownMap.TryGetValue(playerId, out CurrentCooldown currentCooldown)) {

                long remainingSeconds = currentCooldown.GetRemainingSeconds(null);

                if (remainingSeconds > 0) {
                    Log.Info("Cooldown for Player " + player.DisplayName + " still running! " + remainingSeconds + " seconds remaining!");
                    Context.Respond("Command is still on cooldown for " + remainingSeconds + " seconds.");
                    return;
                }

                currentCooldown = CreateNewCooldown(currentCooldownMap, playerId, Plugin.Cooldown);

            } else {

                currentCooldown = CreateNewCooldown(currentCooldownMap, playerId, Plugin.Cooldown);
            }

            if (!CheckConformation(playerId, playerId, gridName, null))
                return;

            try {

                if (Plugin.FixShip(gridName, playerId, Context)) {

                    Log.Info("Cooldown for Player " + player.DisplayName + " started!");
                    currentCooldown.StartCooldown(null);
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

            var currentCooldownMap = Plugin.CurrentCooldownMap;

            if (currentCooldownMap.TryGetValue(playerId, out CurrentCooldown currentCooldown)) {

                long remainingSeconds = currentCooldown.GetRemainingSeconds(null);

                if (remainingSeconds > 0) {
                    Log.Info("Cooldown for Player " + player.DisplayName + " still running! " + remainingSeconds + " seconds remaining!");
                    Context.Respond("Command is still on cooldown for " + remainingSeconds + " seconds.");
                    return;
                }

                currentCooldown = CreateNewCooldown(currentCooldownMap, playerId, Plugin.Cooldown);

            } else {

                currentCooldown = CreateNewCooldown(currentCooldownMap, playerId, Plugin.Cooldown);
            }

            if (!CheckConformation(playerId, playerId, "nogrid", character))
                return;

            try {

                if (Plugin.FixShip(character, playerId, Context)) {

                    Log.Info("Cooldown for Player " + player + " started!");
                    currentCooldown.StartCooldown(null);
                }

            } catch (Exception e) {
                Log.Error(e, "Error on fixing ship");
            }
        }

        private bool CheckConformation(long executingPlayerId, long playerId, string gridName, IMyCharacter character) {

            var confirmationCooldownMap = Plugin.ConfirmationsMap;

            if (confirmationCooldownMap.TryGetValue(executingPlayerId, out CurrentCooldown confirmationCooldown)) {

                long remainingSeconds = confirmationCooldown.GetRemainingSeconds(gridName);

                if (remainingSeconds > 0) {
                    confirmationCooldownMap.Remove(executingPlayerId);
                    return true;
                }
            }
             
            if (!CheckGridFound(playerId, gridName, character))
                return false;

            confirmationCooldown = CreateNewCooldown(confirmationCooldownMap, executingPlayerId, Plugin.CooldownConfirmation);

            Context.Respond("Are you sure you want to continue? Enter the command again within " + Plugin.CooldownConfirmationSeconds + " seconds to confirm.");
            confirmationCooldown.StartCooldown(gridName);

            return false;
        }

        private bool CheckGridFound(long playerId, string gridName, IMyCharacter character) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups;

            if (character == null)
                groups = ShipFixerPlugin.FindGridGroupsForPlayer(gridName, playerId, Plugin.FactionFixEnabled);
            else
                groups = ShipFixerPlugin.FindLookAtGridGroup(character, playerId, Plugin.FactionFixEnabled);

            if (!ShipFixerPlugin.CheckGroups(groups, out _, Context, playerId, Plugin.FactionFixEnabled))
                return false;

            return true;
        }

        private static CurrentCooldown CreateNewCooldown(Dictionary<long, CurrentCooldown> cooldownMap, long playerId, long cooldown) {

            var currentCooldown = new CurrentCooldown(cooldown);

            if (cooldownMap.ContainsKey(playerId))
                cooldownMap[playerId] = currentCooldown;
            else
                cooldownMap.Add(playerId, currentCooldown);

            return currentCooldown;
        }
    }
}
