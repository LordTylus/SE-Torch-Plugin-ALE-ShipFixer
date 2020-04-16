using System;
using System.Collections.Generic;
using System.Text;

namespace ALE_Core.Cooldown {

    public class CooldownManager {

        private readonly Dictionary<ulong, CurrentCooldown> cooldownMap = new Dictionary<ulong, CurrentCooldown>();

        public bool CheckCooldown(ulong steamId, string command, out long remainingSeconds) {

            remainingSeconds = 0;

            if (cooldownMap.TryGetValue(steamId, out CurrentCooldown currentCooldown)) {

                remainingSeconds = currentCooldown.GetRemainingSeconds(command);

                if (remainingSeconds > 0)
                    return false;
            }

            return true;
        }

        public void StartCooldown(ulong steamId, string command, long cooldown) {

            var currentCooldown = new CurrentCooldown(cooldown);

            if (cooldownMap.ContainsKey(steamId))
                cooldownMap[steamId] = currentCooldown;
            else
                cooldownMap.Add(steamId, currentCooldown);

            currentCooldown.StartCooldown(command);
        }

        public void StopCooldown(ulong steamId) {

            if (cooldownMap.ContainsKey(steamId))
                cooldownMap.Remove(steamId);
        }
    }
}