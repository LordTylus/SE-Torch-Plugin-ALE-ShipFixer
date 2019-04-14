using NLog;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using VRage.Groups;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using IMyShipConnector = Sandbox.ModAPI.Ingame.IMyShipConnector;
using IMyShipController = Sandbox.ModAPI.IMyShipController;
using IMyTerminalBlock = Sandbox.ModAPI.Ingame.IMyTerminalBlock;

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

            fixShip(gridName, 0, playerId);
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

            fixShip(gridName, playerId, playerId);
        }

        private void fixShip(string gridName, long playerId, long executingPlayerId) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = findGridGroupsForPlayer(gridName, playerId);

            /* No group or too many groups found */
            if (groups.Count < 1) {
                MyVisualScriptLogicProvider.SendChatMessage("Could not find your Grid.", "Server", executingPlayerId, "Red");
                return;
            }

            /* too many groups found */
            if (groups.Count > 1) {
                MyVisualScriptLogicProvider.SendChatMessage("Found multiple Grids with same Name. Rename your grid first to something unique.", "Server", executingPlayerId, "Red");
                return;
            }

            foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group in groups) {

                foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes) {

                    IMyCubeGrid grid = groupNodes.NodeData;

                    List<IMyTerminalBlock> tBlockList = new List<IMyTerminalBlock>();

                    var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
                    gts.GetBlocksOfType<Sandbox.ModAPI.IMyTerminalBlock>(tBlockList);

                    foreach (var block in tBlockList) {

                        if (block == null)
                            continue;

                        IMyLandingGear landingGear = block as IMyLandingGear;

                        if (landingGear != null && landingGear.IsLocked) {
                            MyVisualScriptLogicProvider.SendChatMessage("Some Landing-Gears are still Locked. Please unlock first!", "Server", executingPlayerId, "Red");
                            return;
                        }

                        IMyShipConnector connector = block as IMyShipConnector;

                        if (connector != null && connector.Status == MyShipConnectorStatus.Connected) {
                            MyVisualScriptLogicProvider.SendChatMessage("Some Connectors are still Locked. Please unlock first!", "Server", executingPlayerId, "Red");
                            return;
                        }

                        IMyShipController controller = block as IMyShipController;

                        if (controller != null && controller.IsUnderControl) {
                            MyVisualScriptLogicProvider.SendChatMessage("Cockpits or Seats are still occupied. Clear them first! Dont forget to check the toilet!", "Server", executingPlayerId, "Red");
                            return;
                        }
                    }
                }
            }

            foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group in groups) {

                List<MyObjectBuilder_EntityBase> objectBuilderList = new List<MyObjectBuilder_EntityBase>();

                foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes) {

                    IMyCubeGrid grid = groupNodes.NodeData;

                    grid.Physics.LinearVelocity = Vector3.Zero;

                    MyObjectBuilder_EntityBase ob = (MyObjectBuilder_EntityBase)grid.GetObjectBuilder();

                    if (!objectBuilderList.Contains(ob)) {

                        objectBuilderList.Add(ob);

                        var entity = grid as IMyEntity;

                        Log.Warn("Grid " + grid.CustomName+" was removed for later paste");

                        MyAPIGateway.Utilities.InvokeOnGameThread(() => entity.Delete());
                        entity.Close();
                        continue;
                    }
                }

                MyAPIGateway.Entities.RemapObjectBuilderCollection(objectBuilderList);
                var ents = new List<IMyEntity>();

                foreach (var ob in objectBuilderList) {

                    if (ob == null)
                        continue;

                    var ent = MyAPIGateway.Entities.CreateFromObjectBuilder(ob);
                    ents.Add(ent);
                }

                MyAPIGateway.Multiplayer.SendEntitiesCreated(objectBuilderList);

                foreach (var ent in ents)
                    MyAPIGateway.Entities.AddEntity(ent, true);
            }
        }

        private ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> findGridGroupsForPlayer(string gridName, long playerId) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = new ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group>();
            Parallel.ForEach(MyCubeGridGroups.Static.Physical.Groups, group => {

                foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes) {

                    IMyCubeGrid grid = groupNodes.NodeData;

                    /* Gridname is wrong ignore */
                    if (!grid.CustomName.Equals(gridName))
                        continue;

                    /* We are not the server and playerId is not owner */
                    if (playerId != 0 && !grid.BigOwners.Contains(playerId))
                        continue;

                    groups.Add(group);
                }
            });

            return groups;
        }
    }

}
