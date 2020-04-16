using System;
using System.Collections.Generic;
using System.Text;

namespace ALE_Core.Cooldown {

    public class CooldownManager {

        private readonly Dictionary<ICooldownKey, CurrentCooldown> cooldownMap = new Dictionary<ICooldownKey, CurrentCooldown>();

        public bool CheckCooldown(ICooldownKey key, string command, out long remainingSeconds) {

            remainingSeconds = 0;

            if (cooldownMap.TryGetValue(key, out CurrentCooldown currentCooldown)) {

                remainingSeconds = currentCooldown.GetRemainingSeconds(command);

                if (remainingSeconds > 0)
                    return false;
            }

            return true;
        }

        public void StartCooldown(ICooldownKey key, string command, long cooldown) {

            var currentCooldown = new CurrentCooldown(cooldown);

            if (cooldownMap.ContainsKey(key))
                cooldownMap[key] = currentCooldown;
            else
                cooldownMap.Add(key, currentCooldown);

            currentCooldown.StartCooldown(command);
        }

        public void StopCooldown(ICooldownKey key) {

            if (cooldownMap.ContainsKey(key))
                cooldownMap.Remove(key);
        }
    }
}