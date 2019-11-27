using NLog;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Concurrent;
using VRage.Groups;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using IMyShipController = Sandbox.ModAPI.IMyShipController;
using IMyTerminalBlock = Sandbox.ModAPI.Ingame.IMyTerminalBlock;
using Torch;
using Torch.API;
using VRage.Game.ModAPI;
using System.Collections.Generic;
using Torch.Commands;
using System.IO;
using System;
using System.Linq;
using Torch.API.Plugins;
using System.Windows.Controls;
using VRage.Game;
using VRage.Game.Entity;
using System.Threading.Tasks;

namespace ALE_ShipFixer {

    public class ShipFixerPlugin : TorchPluginBase, IWpfPlugin {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private Control _control;
        public UserControl GetControl() => _control ?? (_control = new Control(this));


        private Persistent<ShipFixerConfig> _config;
        public ShipFixerConfig Config => _config?.Data;

        public Dictionary<long, CurrentCooldown> CurrentCooldownMap { get; } = new Dictionary<long, CurrentCooldown>();

        public Dictionary<long, CurrentCooldown> ConfirmationsMap { get; } = new Dictionary<long, CurrentCooldown>();

        public long Cooldown { get { return Config.CooldownInSeconds * 1000; } }
        public long CooldownConfirmationSeconds { get { return Config.ConfirmationInSeconds; } }
        public long CooldownConfirmation { get { return Config.ConfirmationInSeconds * 1000; } }
        public bool PlayerCommandEnabled { get { return Config.PlayerCommandEnabled; } }

        /// <inheritdoc />
        public override void Init(ITorchBase torch) {
            base.Init(torch);

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
        }

        public void Save() {
            try {
                _config.Save();
                Log.Info("Configuration Saved.");
            } catch (IOException) {
                Log.Warn("Configuration failed to save");
            }
        }

        public bool FixShip(IMyCharacter character, long playerId, CommandContext Context) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = FindLookAtGridGroup(character, playerId);

            return FixGroups(groups, Context, playerId);
        }

        public bool FixShip(string gridName, long playerId, CommandContext Context) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = FindGridGroupsForPlayer(gridName, playerId);

            return FixGroups(groups, Context, playerId);
        }

        public static bool CheckGroups(ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups, 
            out MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, CommandContext Context, long playerId) {

            /* No group or too many groups found */
            if (groups.Count < 1) {

                Context.Respond("Could not find your Grid. Check if ownership is correct");
                group = null;

                return false;
            }

            /* too many groups found */
            if (groups.Count > 1) {

                Context.Respond("Found multiple Grids with same Name. Rename your grid first to something unique.");
                group = null;

                return false;
            } 
            
            if (!groups.TryPeek(out group)) {
                Context.Respond("Could not work with found grid for unknown reason.");
                return false;
            }

            /* Check if there are Connected grids owned by a different player */
            if (playerId != 0) {

                IMyCubeGrid referenceGrid = null;

                foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes) {

                    IMyCubeGrid grid = groupNodes.NodeData;

                    if (grid.Physics == null)
                        continue;

                    /* We are not the server and playerId is not owner */
                    if (!grid.BigOwners.Contains(playerId))
                        continue;

                    referenceGrid = grid;
                    break;
                }

                if (referenceGrid == null) {
                    Context.Respond("Grid seems to be owned by a different player.");
                    return false;
                }

                foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes) {

                    IMyCubeGrid grid = groupNodes.NodeData;

                    if (grid.Physics == null)
                        continue;

                    if (grid == referenceGrid)
                        continue;

                    if (grid.IsSameConstructAs(referenceGrid))
                        continue;

                    /* We are not the server and playerId is not owner */
                    if (!grid.BigOwners.Contains(playerId)) {
                        Context.Respond("One of the connected grids is owned by a different player.");
                        return false;
                    }
                }
            }

            /* Check if there are people in the cockpit */
            foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes) {

                IMyCubeGrid grid = groupNodes.NodeData;

                List<IMyTerminalBlock> tBlockList = new List<IMyTerminalBlock>();

                var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
                gts.GetBlocksOfType<Sandbox.ModAPI.IMyTerminalBlock>(tBlockList);

                foreach (var block in tBlockList) {
                    if (block == null)
                        continue;


                    if (block is IMyShipController controller && controller.IsUnderControl) {
                        Context.Respond("Cockpits or seats are still occupied! Clear them first and try again. Dont forget to check the toilet!");
                        return false;
                    }
                }
            }

            return true;
        }

        private bool FixGroups(ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups, CommandContext Context, long playerId) {


            if (!CheckGroups(groups, out MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, Context, playerId))
                return false;

            return FixGroup(group, Context);
        }

        private bool FixGroup(MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, CommandContext Context) {

            string playerName = "Server";

            IMyPlayer player = Context.Player;
            if (player != null)
                playerName = player.DisplayName;

            List<MyObjectBuilder_EntityBase> objectBuilderList = new List<MyObjectBuilder_EntityBase>();
            List<MyCubeGrid> gridsList = new List<MyCubeGrid>();

            foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes) {

                MyCubeGrid grid = groupNodes.NodeData;
                gridsList.Add(grid);

                grid.Physics.LinearVelocity = Vector3.Zero;

                MyObjectBuilder_EntityBase ob = grid.GetObjectBuilder(true);

                if (!objectBuilderList.Contains(ob)) {

                    if (Config.RemoveBlueprintsFromProjectors)
                        if (ob is MyObjectBuilder_CubeGrid gridBuilder)
                            foreach (MyObjectBuilder_CubeBlock cubeBlock in gridBuilder.CubeBlocks)
                                if (cubeBlock is MyObjectBuilder_ProjectorBase projector)
                                    projector.ProjectedGrids = null;

                    objectBuilderList.Add(ob);
                }
            }

            foreach (MyCubeGrid grid in gridsList) {

                var entity = grid as IMyEntity;

                Log.Warn("Player " + playerName + " used ShipFixerPlugin on Grid " + grid.DisplayName + " for cut & paste!");

                entity.Delete();
                entity.Close();
            }

            MyAPIGateway.Entities.RemapObjectBuilderCollection(objectBuilderList);

            bool hasMultipleGrids = objectBuilderList.Count > 1;

            if (!hasMultipleGrids) {

                foreach (var ob in objectBuilderList)
                    MyEntities.CreateFromObjectBuilderParallel(ob, true);

            } else {

                MyEntities.Load(objectBuilderList);
            }

            Context.Respond("Ship was fixed!");
            return true;
        }

        public static ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> FindLookAtGridGroup(IMyCharacter controlledEntity, long playerId) {

            const float range = 5000;
            Matrix worldMatrix;
            Vector3D startPosition;
            Vector3D endPosition;

            worldMatrix = controlledEntity.GetHeadMatrix(true, true, false); // dead center of player cross hairs, or the direction the player is looking with ALT.
            startPosition = worldMatrix.Translation + worldMatrix.Forward * 0.5f;
            endPosition = worldMatrix.Translation + worldMatrix.Forward * (range + 0.5f);

            var list = new Dictionary<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group, double>();
            var ray = new RayD(startPosition, worldMatrix.Forward);

            foreach(var group in MyCubeGridGroups.Static.Physical.Groups) {

                foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes) {

                    IMyCubeGrid cubeGrid = groupNodes.NodeData;

                    if (cubeGrid != null) {

                        if (cubeGrid.Physics == null)
                            continue;

                        /* We are not the server and playerId is not owner */
                        if (playerId != 0 && !cubeGrid.BigOwners.Contains(playerId))
                            continue;

                        // check if the ray comes anywhere near the Grid before continuing.    
                        if (ray.Intersects(cubeGrid.WorldAABB).HasValue) {

                            Vector3I? hit = cubeGrid.RayCastBlocks(startPosition, endPosition);

                            if (hit.HasValue) {

                                double distance = (startPosition - cubeGrid.GridIntegerToWorld(hit.Value)).Length();

                                if (list.TryGetValue(group, out double oldDistance)) {

                                    if (distance < oldDistance) {
                                        list.Remove(group);
                                        list.Add(group, distance);
                                    }

                                } else {

                                    list.Add(group, distance);
                                }
                            }
                        }
                    }
                }
            }

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> bag = new ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group>();

            if (list.Count == 0) 
                return bag;

            // find the closest Entity.
            var item = list.OrderBy(f => f.Value).First();
            bag.Add(item.Key);

            return bag;
        }

        public static ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> FindGridGroupsForPlayer(string gridName, long playerId) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups = new ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group>();
            Parallel.ForEach(MyCubeGridGroups.Static.Physical.Groups, group => {

                foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes) {

                    IMyCubeGrid grid = groupNodes.NodeData;

                    if (grid.Physics == null)
                        continue;

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