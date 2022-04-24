using NLog;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Groups;
using VRage.ObjectBuilders;
using VRageMath;
using IMyShipController = Sandbox.ModAPI.IMyShipController;
using IMyTerminalBlock = Sandbox.ModAPI.Ingame.IMyTerminalBlock;
using VRage.Game.ModAPI;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using System.Threading.Tasks;
using ALE_Core.Utils;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.World;
using Sandbox.Game.Entities.Blocks;

namespace ALE_ShipFixer {
    public class ShipFixerCore {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static ShipFixerCore Instance;

        public static void Init() {
            Instance = new ShipFixerCore();
        }

        public CheckResult FixShip(IMyCharacter character, long playerId) {

            var groups = FindLookAtGridGroup(character, playerId, ShipFixerPlugin.Instance.FactionFixEnabled);

            return FixGroups(groups, playerId);
        }

        public CheckResult FixShip(string gridName, long playerId) {

            var groups = FindGridGroupsForPlayer(gridName, playerId, ShipFixerPlugin.Instance.FactionFixEnabled);

            return FixGroups(groups, playerId);
        }

        public static CheckResult CheckGroups(List<MyCubeGrid> groups, out List<MyCubeGrid> group, long playerId, bool factionFixEnabled) {

            /* No group or too many groups found */
            if (groups.Count < 1) {
                group = null;
                return CheckResult.TOO_FEW_GRIDS;
            }

            group = groups;

            /* Check if there are Connected grids owned by a different player */
            if (playerId != 0) {

                MyCubeGrid referenceGrid = null;

                foreach (var grid in groups) {

                    if (grid.Physics == null)
                        continue;

                    /* We are not the server and playerId is not owner */
                    if (!OwnershipCorrect(grid, playerId, factionFixEnabled))
                        continue;

                    referenceGrid = grid;
                    break;
                }

                if (referenceGrid == null)
                    return CheckResult.OWNED_BY_DIFFERENT_PLAYER;

                foreach (var grid in groups) {

                    if (grid.Physics == null)
                        continue;

                    if (grid == referenceGrid)
                        continue;

                    if (grid.IsSameConstructAs(referenceGrid))
                        continue;

                    /* We are not the server and playerId is not owner */
                    if (!OwnershipCorrect(grid, playerId, factionFixEnabled))
                        return CheckResult.OWNED_BY_DIFFERENT_PLAYER;
                }
            }

            Dictionary<long, MyPlayer> dictionary = new Dictionary<long, MyPlayer>();
            var PlayerRemoved = false;

            if (MySession.Static.Players.GetOnlinePlayers().Count > 0)
                dictionary = MySession.Static.Players.GetOnlinePlayers().ToDictionary((MyPlayer b) => b.Identity.IdentityId);

            var GridOwnerFaction = MySession.Static.Factions.TryGetPlayerFaction(groups[0].BigOwners.FirstOrDefault());

            /* Check if there are people in the cockpit */
            foreach (var grid in groups) {

                List<IMyTerminalBlock> tBlockList = new List<IMyTerminalBlock>();

                var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);

                gts.GetBlocksOfType<Sandbox.ModAPI.IMyTerminalBlock>(tBlockList);

                foreach (var block in tBlockList) {

                    if (block == null)
                        continue;

                    if (block is IMyShipController controller && controller.IsUnderControl) {

                        var PlayerInControl = controller.ControllerInfo?.ControllingIdentityId;
                        var ControllingPlayerFaction = MySession.Static.Factions.TryGetPlayerFaction((long)PlayerInControl);

                        /* Check if controlling player is online, if not we can fixship */
                        if (PlayerInControl != null && dictionary.ContainsKey((long)PlayerInControl)) {

                            if (GridOwnerFaction != null && ControllingPlayerFaction != null) {

                                var FactionsRelationship = MySession.Static.Factions.GetRelationBetweenFactions(GridOwnerFaction.FactionId, ControllingPlayerFaction.FactionId);

                                if (GridOwnerFaction.FactionId == ControllingPlayerFaction.FactionId || FactionsRelationship.Item1 != MyRelationsBetweenFactions.Enemies) {

                                    if (controller.Pilot != null && controller is MyCockpit cockpit) {
                                        cockpit.RemovePilot();
                                        PlayerRemoved = true;
                                    }

                                    if (controller.Pilot != null && controller is MyCryoChamber CryoChamber) {
                                        CryoChamber.RemovePilot();
                                        PlayerRemoved = true;
                                    }
                                }
                            }

                            if (!PlayerRemoved)
                                return CheckResult.GRID_OCCUPIED;
                        }
                    }
                }
            }

            return CheckResult.OK;
        }

        private CheckResult FixGroups(List<MyCubeGrid> groups, long playerId) {

            var result = CheckGroups(groups, out List<MyCubeGrid> group, playerId, ShipFixerPlugin.Instance.FactionFixEnabled);

            if (result != CheckResult.OK)
                return result;

            MyIdentity executingPlayer = null;

            if (playerId != 0)
                executingPlayer = PlayerUtils.GetIdentityById(playerId);

            return FixGroup(group, executingPlayer);
        }

        private CheckResult FixGroup(List<MyCubeGrid> GridGroups, MyIdentity executingPlayer) {

            string playerName = "Server";

            if (executingPlayer != null)
                playerName = executingPlayer.DisplayName;

            List<MyObjectBuilder_EntityBase> objectBuilderList = new List<MyObjectBuilder_EntityBase>();
            List<MyCubeGrid> gridsList = new List<MyCubeGrid>();
            SpawnCounter.SpawnCallback counter = null;

            foreach (var grid in GridGroups) {

                gridsList.Add(grid);
                grid.Physics.ClearSpeed();
                MyObjectBuilder_EntityBase ob = grid.GetObjectBuilder(true);

                if (!objectBuilderList.Contains(ob)) {
                    if (ob is MyObjectBuilder_CubeGrid gridBuilder) {
                        foreach (MyObjectBuilder_CubeBlock cubeBlock in gridBuilder.CubeBlocks) {

                            if (ShipFixerPlugin.Instance.Config.RemoveBlueprintsFromProjectors)
                                if (cubeBlock is MyObjectBuilder_ProjectorBase projector)
                                    projector.ProjectedGrids = null;

                            if (cubeBlock is MyObjectBuilder_OxygenTank o2Tank)
                                o2Tank.AutoRefill = false;
                        }
                    }

                    objectBuilderList.Add(ob);
                }
            }

            foreach (MyCubeGrid grid in gridsList) {

                Log.Warn("Player " + playerName + " used ShipFixerPlugin on Grid " + grid.DisplayName + " for cut & paste!");

                grid.Close();
            }

            MyAPIGateway.Entities.RemapObjectBuilderCollection(objectBuilderList);

            counter = new SpawnCounter.SpawnCallback(objectBuilderList.Count);

            foreach (var ObGrid in objectBuilderList) 
                MyAPIGateway.Entities.CreateFromObjectBuilderParallel(ObGrid, false, counter.Increment);

            return CheckResult.SHIP_FIXED;
        }

        public static List<MyCubeGrid> FindLookAtGridGroup(IMyCharacter controlledEntity, long playerId, bool factionFixEnabled) {

            const float range = 5000;
            Matrix worldMatrix;
            Vector3D startPosition;
            Vector3D endPosition;

            worldMatrix = controlledEntity.GetHeadMatrix(true, true, false); // dead center of player cross hairs, or the direction the player is looking with ALT.
            startPosition = worldMatrix.Translation + worldMatrix.Forward * 0.5f;
            endPosition = worldMatrix.Translation + worldMatrix.Forward * (range + 0.5f);

            var list = new Dictionary<MyCubeGrid, double>();
            var ray = new RayD(startPosition, worldMatrix.Forward);

            Parallel.ForEach(MyCubeGridGroups.Static.Physical.Groups, group => {
                foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes) {
                    MyCubeGrid cubeGrid = groupNodes.NodeData;

                    if (cubeGrid != null) {
                        if (cubeGrid.Physics == null)
                            continue;

                        /* We are not the server and playerId is not owner */
                        if (playerId != 0 && !OwnershipCorrect(cubeGrid, playerId, factionFixEnabled))
                            continue;

                        // check if the ray comes anywhere near the Grid before continuing.    
                        if (ray.Intersects(cubeGrid.PositionComp.WorldAABB).HasValue) {
                            Vector3I? hit = cubeGrid.RayCastBlocks(startPosition, endPosition);

                            if (hit.HasValue) {
                                double distance = (startPosition - cubeGrid.GridIntegerToWorld(hit.Value)).Length();

                                if (list.TryGetValue(cubeGrid, out double oldDistance)) {

                                    if (distance < oldDistance) {
                                        list.Remove(cubeGrid);
                                        list.Add(cubeGrid, distance);
                                    }

                                } else {
                                    list.Add(cubeGrid, distance);
                                }
                            }
                        }
                    }
                }
            });

            var GridsGroup = new List<MyCubeGrid>();

            // find the closest Entity.
            if (list != null && list.Any()) {

                var grid = list.OrderBy(f => f.Value).First().Key;

                // only here we can see attached by landing gear grids to main grid!
                var IMygrids = new List<IMyCubeGrid>();
                MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Physical, IMygrids);

                // convert back to MyCubeGrid
                foreach (var Mygrid in IMygrids) 
                    GridsGroup.Add((MyCubeGrid)Mygrid);
                
                // sort the list. largest to smallest
                GridsGroup.SortNoAlloc((x, y) => x.BlocksCount.CompareTo(y.BlocksCount));
                GridsGroup.Reverse();
                GridsGroup.SortNoAlloc((x, y) => x.GridSizeEnum.CompareTo(y.GridSizeEnum));

                return GridsGroup;
            }

            return GridsGroup;
        }

        public static List<MyCubeGrid> FindGridGroupsForPlayer(string gridName, long playerId, bool factionFixEnabled) {
       
            List<MyCubeGrid> GridsGroup = new List<MyCubeGrid>();

            Parallel.ForEach(MyCubeGridGroups.Static.Physical.Groups, group => {

                foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes) {
                    MyCubeGrid grid = groupNodes.NodeData;

                    if (grid.Physics == null)
                        continue;

                    /* Gridname is wrong ignore */
                    if (!grid.DisplayName.Equals(gridName))
                        continue;

                    /* We are not the server and playerId is not owner */
                    if (playerId != 0 && !OwnershipCorrect(grid, playerId, factionFixEnabled))
                        continue;

                    GridsGroup.Add(grid);
                    break;
                }
            });

            // only here we can see attached by landing gear grids to main grid!
            var IMygrids = new List<IMyCubeGrid>();
            MyAPIGateway.GridGroups.GetGroup(GridsGroup.FirstOrDefault(), GridLinkTypeEnum.Physical, IMygrids);

            GridsGroup.Clear();

            // convert back to MyCubeGrid
            foreach (var Mygrid in IMygrids) 
                GridsGroup.Add((MyCubeGrid)Mygrid);
            
            // sort the list. largest to smallest
            GridsGroup.SortNoAlloc((x, y) => x.BlocksCount.CompareTo(y.BlocksCount));
            GridsGroup.Reverse();
            GridsGroup.SortNoAlloc((x, y) => x.GridSizeEnum.CompareTo(y.GridSizeEnum));

            return GridsGroup;
        }

        public static bool OwnershipCorrect(MyCubeGrid grid, long playerId, bool checkFactions) {

            /* If Player is owner we are totally fine and can allow it */
            if (grid.BigOwners.Contains(playerId))
                return true;

            /* If he is not owner and we dont want to allow checks for faction members... then prohibit */
            if (!checkFactions)
                return false;

            /* If checks for faction are allowed grab owner and see if factions are equal */
            long gridOwner = OwnershipUtils.GetOwner(grid);

            return FactionUtils.HavePlayersSameFaction(playerId, gridOwner);
        }
    }
}
