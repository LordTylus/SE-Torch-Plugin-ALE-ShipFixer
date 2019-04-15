using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALE_ShipFixer {
    public class CurrentCooldown {

        private static readonly long COOLDOWN = 15 * 60 * 1000;

        private long _playerId;
        private long _startTime;
        private long _currentCooldown;

        public CurrentCooldown(long playerID) {
            this._playerId = playerID;
        }

        public void startCooldown() {

            this._startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            this._currentCooldown = COOLDOWN;
        }

        public long getRemainingSeconds() {

            long elapsedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _startTime;

            if (elapsedTime >= _currentCooldown) 
                return 0;

            return (_currentCooldown - elapsedTime) / 1000;
        }
    }
}
