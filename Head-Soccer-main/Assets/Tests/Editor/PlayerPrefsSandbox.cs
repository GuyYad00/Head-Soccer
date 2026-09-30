using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeadSoccer.Tests
{
    /// <summary>
    /// Snapshots a set of PlayerPrefs keys, clears them so a test starts from the
    /// game's defaults, and restores the originals when disposed. The tests run in the
    /// editor against the same PlayerPrefs the game uses, so without this a test run
    /// would wipe the player's chosen character, keys and records.
    /// </summary>
    internal sealed class PlayerPrefsSandbox : IDisposable
    {
        private readonly Dictionary<string, int> savedInts = new Dictionary<string, int>();
        private readonly Dictionary<string, string> savedStrings = new Dictionary<string, string>();
        private readonly List<string> intKeys;
        private readonly List<string> stringKeys;

        public PlayerPrefsSandbox(IEnumerable<string> intKeys, IEnumerable<string> stringKeys = null)
        {
            this.intKeys = new List<string>(intKeys);
            this.stringKeys = new List<string>(stringKeys ?? Array.Empty<string>());

            foreach (string key in this.intKeys)
            {
                if (PlayerPrefs.HasKey(key)) savedInts[key] = PlayerPrefs.GetInt(key);
                PlayerPrefs.DeleteKey(key);
            }
            foreach (string key in this.stringKeys)
            {
                if (PlayerPrefs.HasKey(key)) savedStrings[key] = PlayerPrefs.GetString(key);
                PlayerPrefs.DeleteKey(key);
            }
        }

        public void Dispose()
        {
            foreach (string key in intKeys)
            {
                if (savedInts.TryGetValue(key, out int value)) PlayerPrefs.SetInt(key, value);
                else PlayerPrefs.DeleteKey(key);
            }
            foreach (string key in stringKeys)
            {
                if (savedStrings.TryGetValue(key, out string value)) PlayerPrefs.SetString(key, value);
                else PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();
        }
    }
}
