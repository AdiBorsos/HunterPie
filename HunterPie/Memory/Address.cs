﻿using System;
using System.Collections.Generic;
using System.IO;
using HunterPie.Logger;

namespace HunterPie.Memory {
    class Address {
        public class Offsets {
            public static Int64[] LevelOffsets = new Int64[5] { 0x70, 0x68, 0x8, 0x20, 0x108 };

            public static Int64[] WeaponOffsets = new Int64[5] { 0x70, 0x5A8, 0x310, 0x148, 0x2B8 };

            public static Int64[] SessionOffsets = new Int64[5] { 0xA0, 0x20, 0x80, 0x9C, 0x3C8 };

            public static Int64[] EquipmentOffsets = new Int64[5] { 0x78, 0x50, 0x40, 0x450, 0x0 };

            public static Int64[] PartyOffsets = new Int64[2] { 0x0, 0x0 };

            public static Int64[] MonsterOffsets = new Int64[3] { 0xAF738, 0x47CDE0, 0x0 };
            public static Int64 NextMonsterPtr = 0x28;
            public static Int64 MonsterHPComponentOffset = 0x129D8 + 0x48;
            public static Int64 MonsterNamePtr = 0x290;

            public static Int64 FertilizersOffset = 0x6740C;
            public static Int64 HarvestBoxOffset = 0x10;
        }
        public static int PREICEBORNE_VERSION = 168031;
        public static int GAME_VERSION = 168031;

        // Static addresses
        public static Int64 BASE = 0x140000000;
        public static Int64 LEVEL_OFFSET = 0x3B4C2B8;
        public static Int64 ZONE_OFFSET = 0x48F2E60;
        public static Int64 MONSTER_OFFSET = 0x48E06E8;
        public static Int64 SESSION_OFFSET = 0x48E85C0;
        public static Int64 EQUIPMENT_OFFSET = 0x3B50668;
        public static Int64 WEAPON_OFFSET = 0x3BEDE58;
        public static Int64 PARTY_OFFSET = 0x48E1850;

        // Consts
        public const Int64 cooldownFixed = 0x9EC;
        public const Int64 cooldownDynamic = 0x99C;
        public const Int64 timerFixed = 0xADC;
        public const Int64 timerDynamic = 0xA8C;

        // Loaded values
        private static Dictionary<string, Int64> MappedAddresses = new Dictionary<string, Int64>();
        private static Dictionary<string, Int64[]> MappedOffsets = new Dictionary<string, Int64[]>();
        
        public static bool LoadMemoryMap(int version) {
            string FILE_NAME = $"MonsterHunterWorld.{version}.map";
            // If dir or file doesn't exist
            if (!Directory.Exists("address")) return false;
            if (!File.Exists($"address/{FILE_NAME}")) return false;
            // Load file
            if (!isOlderThanIceborne(version)) {
                // Update offsets for iceborne
                UpdateToIceborneOffsets();
            }
            LoadMemoryAddresses(FILE_NAME);
            GAME_VERSION = version;
            LoadValuesToMemory();
            return true;
        }

        private static void LoadMemoryAddresses(string filename) {
            // Clear all loaded values
            MappedAddresses.Clear();
            MappedOffsets.Clear();
            string[] fileLines = File.ReadAllLines($"address/{filename}");
            foreach (string line in fileLines) {
                if (line.StartsWith("#")) continue; // Ignore comments
                string[] parsed = line.Split(' ');
                // parsed[0]: type
                // parsed[1]: name
                // parsed[2]: value
                AddValueToMap(parsed[0], parsed);
            }
        }

        private static void AddValueToMap(string type, string[] values) {
            string name = values[1];
            string value = values[2];
            switch(type) {
                case "Address":
                case "long":
                    Int64 parsedValue;
                    try {
                        parsedValue = ParseHex(value);
                    } catch {
                        Debugger.Error($"Failed parsing value for \"{name}\"");
                        parsedValue = 0xFFFFFFFF;
                    }
                    MappedAddresses.Add(name, parsedValue);
                    break;
                case "Offset":
                    string[] strOffsets = value.Split(',');
                    Int64[] offsets = new Int64[strOffsets.Length];
                    for (int i = 0; i < strOffsets.Length; i++) {
                        try {
                            offsets[i] = ParseHex(strOffsets[i]);
                        } catch {
                            Debugger.Error($"Failed to parse value {strOffsets[i]}");
                            offsets[i] = 0xFF;
                        }
                    }
                    MappedOffsets.Add(name, offsets);
                    break;
                default:
                    Debugger.Error($"Invalid type: {type}");
                    break;
            }
        }

        private static Int64 ParseHex(string hexstring) {
            Int64 result = Convert.ToInt64(hexstring.Replace("0x", ""), 16);
            return result;
        }

        private static void LoadValuesToMemory() {
            LoadAddressFromDict(nameof(BASE), out BASE, BASE);
            LoadAddressFromDict(nameof(LEVEL_OFFSET), out LEVEL_OFFSET, LEVEL_OFFSET);
            LoadAddressFromDict(nameof(MONSTER_OFFSET), out MONSTER_OFFSET, MONSTER_OFFSET);
            LoadAddressFromDict(nameof(EQUIPMENT_OFFSET), out EQUIPMENT_OFFSET, EQUIPMENT_OFFSET);
            LoadAddressFromDict(nameof(WEAPON_OFFSET), out WEAPON_OFFSET, WEAPON_OFFSET);
            // Add this in the next version
            //LoadAddressFromDict(nameof(SESSION_OFFSET), out SESSION_OFFSET, SESSION_OFFSET);
            //LoadAddressFromDict(nameof(PARTY_OFFSET), out PARTY_OFFSET, PARTY_OFFSET);
            // Load offsets
            LoadOffsetsFromDict("LevelOffsets", out Offsets.LevelOffsets, Offsets.LevelOffsets);
            LoadOffsetsFromDict("SessionOffsets", out Offsets.SessionOffsets, Offsets.SessionOffsets);
            LoadOffsetsFromDict("WeaponOffsets", out Offsets.WeaponOffsets, Offsets.WeaponOffsets);
            LoadOffsetsFromDict("MonsterOffsets", out Offsets.MonsterOffsets, Offsets.MonsterOffsets);
            LoadOffsetsFromDict("EquipmentOffsets", out Offsets.EquipmentOffsets, Offsets.EquipmentOffsets);
        }

        private static void LoadAddressFromDict(string name, out Int64 variable, Int64 oldValue) {
            try {
                variable = MappedAddresses[name] == 0xFFFFFFFF ? oldValue : MappedAddresses[name];
            } catch {
                variable = oldValue;
                Debugger.Error($"MonsterHunterWorld.{GAME_VERSION}.map missing value for {name}");
            }
        }

        private static void LoadOffsetsFromDict(string name, out Int64[] offsetsArray, Int64[] oldOffsetsArray) {
            try {
                offsetsArray = MappedOffsets[name];
            } catch {
                offsetsArray = oldOffsetsArray;
                Debugger.Error($"MonsterHunterWorld.{GAME_VERSION}.map missing offsets for {name}");
            }
        }

        // Support Iceborne and older versions
        private static bool isOlderThanIceborne(int game_version) {
            if (game_version <= PREICEBORNE_VERSION) {
                Debugger.Error("Pre-Iceborne game not supported anymore.");
            }
            return game_version <= PREICEBORNE_VERSION;
        }

        private static void UpdateToIceborneOffsets() {
            Offsets.FertilizersOffset = 0x102FE4;
            Offsets.HarvestBoxOffset = 0x20;

            // Monster Iceborne offsets
            Offsets.NextMonsterPtr = 0x10;
            Offsets.MonsterHPComponentOffset = 0x76B0;
            Offsets.MonsterNamePtr = 0x2e0;
            Debugger.Warn("Updated offsets to Iceborne's version");
        }
    }
}
