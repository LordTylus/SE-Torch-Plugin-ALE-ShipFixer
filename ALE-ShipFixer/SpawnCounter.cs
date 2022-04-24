using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.ModAPI;

namespace ALE_ShipFixer {

    public static class SpawnCounter {

        public class SpawnCallback {

            private int _counter;
            private readonly List<IMyEntity> _entlist;
            private readonly int _maxCount;

            public SpawnCallback(int count) {
                _counter = 0;
                _entlist = new List<IMyEntity>();
                _maxCount = count;
            }

            public void Increment(IMyEntity ent) {
                _counter++;
                _entlist.Add(ent);

                if (_counter < _maxCount)
                    return;

                FinalSpawnCallback(_entlist);
            }

            private static void FinalSpawnCallback(List<IMyEntity> grids) {
                foreach (MyCubeGrid ent in grids) {
                    ent.DetectDisconnectsAfterFrame();
                    MyAPIGateway.Entities.AddEntity(ent, true);
                }
            }
        }
    }
}
