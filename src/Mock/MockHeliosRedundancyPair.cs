using System;
using System.Collections.Generic;
using System.Linq;

namespace PepperDash.Essentials.Plugins
{
    /// <summary>
    /// The redundancy state that two <see cref="MockMegapixelHeliosDevice"/>s with the same <c>mockPairKey</c>
    /// share, modelled on the Helios emulator's pairs (inferred, not captured from a real pair):
    /// <list type="bullet">
    /// <item>The pair has one <c>desired</c> controller (<c>main</c> or <c>backup</c>) and the mode <c>failover</c>.
    /// Setting the active controller on either unit sets it, and so moves both.</item>
    /// <item>Each unit has its own role (<c>main</c>, <c>backup</c> or <c>offline</c>); its state is computed:
    /// a unit's state override wins; then the pair's mixed flag makes both <c>mixed</c>; an offline or failed
    /// (comm error) unit is <c>standby</c>; two live units claiming the same role are both <c>mixed</c>; the unit
    /// whose role is desired is <c>active</c>; if no live unit has the desired role, the remaining live unit is
    /// <c>active</c> (failover); otherwise <c>standby</c>.</item>
    /// </list>
    /// A mock with no pair key gets a pair of its own and doesn't fail over, so it reports <c>active</c> only
    /// when its role is the desired one, as the mock always has.
    /// </summary>
    internal class MockHeliosRedundancyPair
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<string, MockHeliosRedundancyPair> Pairs =
            new Dictionary<string, MockHeliosRedundancyPair>(StringComparer.OrdinalIgnoreCase);

        private readonly List<MockMegapixelHeliosDevice> _members = new List<MockMegapixelHeliosDevice>();
        private string _desired = "main";
        private bool _mixed;

        private MockHeliosRedundancyPair(string key, bool isShared)
        {
            Key = key;
            IsShared = isShared;
        }

        /// <summary>The pair key, or the unit's own key for an unpaired mock.</summary>
        public string Key { get; }

        /// <summary>True when the pair came from a <c>mockPairKey</c>, so its units can fail over.</summary>
        public bool IsShared { get; }

        public string Mode => "failover";

        public string Desired
        {
            get { lock (Sync) return _desired; }
        }

        /// <summary>Join the pair named <paramref name="pairKey"/>, or a pair of its own when that is empty.</summary>
        /// <returns>The pair, and how many units it now has.</returns>
        public static MockHeliosRedundancyPair Join(string pairKey, MockMegapixelHeliosDevice unit, out int memberCount)
        {
            lock (Sync)
            {
                MockHeliosRedundancyPair pair;
                if (string.IsNullOrEmpty(pairKey))
                {
                    pair = new MockHeliosRedundancyPair(unit.Key, false);
                }
                else if (!Pairs.TryGetValue(pairKey, out pair))
                {
                    pair = new MockHeliosRedundancyPair(pairKey, true);
                    Pairs[pairKey] = pair;
                }

                pair._members.Add(unit);
                memberCount = pair._members.Count;
                return pair;
            }
        }

        /// <summary>The state <paramref name="unit"/> reports: <c>active</c>, <c>standby</c> or <c>mixed</c>.</summary>
        public string StateFor(MockMegapixelHeliosDevice unit)
        {
            lock (Sync)
            {
                if (!string.IsNullOrEmpty(unit.MockStateOverride)) return unit.MockStateOverride;
                if (_mixed) return "mixed";
                if (unit.MockRole == "offline" || !unit.MockIsLive) return "standby";

                var live = _members.Where(m => m.MockRole != "offline" && m.MockIsLive).ToList();
                if (live.Count(m => m.MockRole == unit.MockRole) > 1) return "mixed";
                if (unit.MockRole == _desired) return "active";
                if (IsShared && !live.Any(m => m.MockRole == _desired)) return "active";
                return "standby";
            }
        }

        /// <summary>Make <paramref name="desired"/> (<c>main</c> or <c>backup</c>) the pair's active controller.</summary>
        public void SetDesired(string desired)
        {
            lock (Sync) _desired = desired;
            NotifyMembers();
        }

        /// <summary>Turn the pair-wide mixed state on or off (both units report <c>mixed</c>).</summary>
        public void SetMixed(bool mixed)
        {
            lock (Sync) _mixed = mixed;
            NotifyMembers();
        }

        /// <summary>Tell every unit to refresh its redundancy feedback, after anything that can move the pair.</summary>
        public void NotifyMembers()
        {
            List<MockMegapixelHeliosDevice> members;
            lock (Sync) members = _members.ToList();

            foreach (var member in members)
                member.FireRedundancyFeedbacks();
        }

        /// <summary>One line for the console, e.g. <c>pair-1: vwp-01 (main, standby) + vwp-02 (backup, active) desired=backup mixed=off</c>.</summary>
        public string Describe()
        {
            List<MockMegapixelHeliosDevice> members;
            bool mixed;
            string desired;
            lock (Sync)
            {
                members = _members.ToList();
                mixed = _mixed;
                desired = _desired;
            }

            var units = string.Join(" + ", members.Select(m => $"{m.Key} ({m.MockRole}, {StateFor(m)})"));
            return $"{Key}: {units}  desired={desired} mode={Mode} mixed={(mixed ? "on" : "off")}";
        }
    }
}
