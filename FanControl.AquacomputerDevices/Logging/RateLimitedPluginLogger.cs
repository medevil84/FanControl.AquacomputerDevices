using System;
using System.Collections.Generic;
using FanControl.Plugins;

namespace FanControl.AquacomputerDevices.Logging
{
    /// <summary>
    /// Prevents flooding FanControl's service_log.txt.
    /// First occurrence is logged; identical messages are dropped for 10 minutes.
    /// At most 30 distinct new messages per minute. No periodic "suppressed N"
    /// summary (that itself was spamming the log every minute).
    /// </summary>
    internal sealed class RateLimitedPluginLogger : IPluginLogger
    {
        private readonly IPluginLogger inner;
        private readonly object gate = new object();
        private readonly Dictionary<string, DateTime> lastSeen = new Dictionary<string, DateTime>();
        private readonly TimeSpan duplicateInterval = TimeSpan.FromMinutes(10);
        private DateTime windowStart = DateTime.UtcNow;
        private int emitted;
        private const int MaxPerMinute = 30;
        private const int MaxTracked = 256;

        internal RateLimitedPluginLogger(IPluginLogger innerLogger)
        {
            inner = innerLogger;
        }

        public void Log(string message)
        {
            if (inner == null || string.IsNullOrEmpty(message)) return;
            bool forward = false;
            DateTime now = DateTime.UtcNow;
            lock (gate)
            {
                if ((now - windowStart) >= TimeSpan.FromMinutes(1))
                {
                    windowStart = now;
                    emitted = 0;
                    var expired = new List<string>();
                    foreach (var pair in lastSeen)
                        if ((now - pair.Value) >= duplicateInterval) expired.Add(pair.Key);
                    foreach (var key in expired) lastSeen.Remove(key);
                }

                DateTime last;
                if (lastSeen.TryGetValue(message, out last) && (now - last) < duplicateInterval)
                {
                    // silent drop
                }
                else if (emitted >= MaxPerMinute)
                {
                    // silent drop
                }
                else
                {
                    if (lastSeen.Count >= MaxTracked)
                    {
                        string drop = null;
                        foreach (var key in lastSeen.Keys) { drop = key; break; }
                        if (drop != null) lastSeen.Remove(drop);
                    }
                    lastSeen[message] = now;
                    emitted++;
                    forward = true;
                }
            }

            if (!forward) return;
            try { inner.Log(message); }
            catch { }
        }
    }
}
