using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALE_ShipFixer {
    public class CurrentCooldown {

        private long _startTime;
        private long _currentCooldown;

        private String command;

        public CurrentCooldown(long cooldown) {
            this._currentCooldown = cooldown;
        }

        public void startCooldown(String command) {
            this.command = command;
            this._startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public long getRemainingSeconds(String command) {

            if (this.command != command)
                return 0;

            long elapsedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _startTime;

            if (elapsedTime >= _currentCooldown) 
                return 0;

            return (_currentCooldown - elapsedTime) / 1000;
        }
    }
}
