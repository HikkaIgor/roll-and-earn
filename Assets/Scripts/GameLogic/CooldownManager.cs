using System;
using System.Collections.Generic;
using UnityEngine;

namespace RollAndEarn
{
    public class CooldownManager : MonoBehaviour
    {
        private Dictionary<byte, long> _cooldownExpiries = new();

        public void SetCooldown(byte adventureType, long expiryTimestamp)
        {
            _cooldownExpiries[adventureType] = expiryTimestamp;
        }

        public bool IsOnCooldown(byte adventureType)
        {
            if (!_cooldownExpiries.TryGetValue(adventureType, out var expiry)) return false;
            long now = PlayerProfile.ValidatorTime > 0 ? PlayerProfile.ValidatorTime : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return now < expiry;
        }

        public float GetRemainingSeconds(byte adventureType)
        {
            if (!_cooldownExpiries.TryGetValue(adventureType, out var expiry)) return 0f;
            long now = PlayerProfile.ValidatorTime > 0 ? PlayerProfile.ValidatorTime : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long remaining = expiry - now;
            return remaining > 0 ? remaining : 0f;
        }

        public void SyncFromProfile(PlayerProfile profile)
        {
            if (profile == null) return;
            for (byte i = 0; i < 3; i++)
                _cooldownExpiries[i] = profile.cooldownExpiries[i];
        }

        public void ClearAll()
        {
            _cooldownExpiries.Clear();
        }
    }
}
