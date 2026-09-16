using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;

namespace PeakTrollMod
{
    internal sealed class BiggerBackpackManager
    {
        internal const int BackpackSlots = 8;
        internal const int FannyPackSlots = 4;
        private const int NativeBackpackSlots = 4;
        private const int NativeFannyPackSlots = 2;

        private readonly ModConfig _settings;
        private readonly ManualLogSource _log;
        private static readonly FieldInfo WheelSlicesField = AccessTools.Field(typeof(BackpackWheel), "slices");
        private float _nextErrorLog;
        private string _status = "Off";

        public BiggerBackpackManager(ModConfig settings, ManualLogSource log)
        {
            _settings = settings;
            _log = log;
        }

        public bool Enabled
        {
            get { return _settings.BiggerBackpackEnabled.Value; }
            set
            {
                _settings.BiggerBackpackEnabled.Value = value;
                _status = value ? "Active — Backpack 8 slots, Fanny Pack 4 slots." : "Off — occupied extra slots remain accessible until emptied.";
                if (!value) TryRestoreLocalCapacity();
            }
        }

        public string Status { get { return _status; } }

        public void Tick()
        {
            Character character = Character.localCharacter;
            if (character == null || character.player == null) return;
            PrepareEquipped(character.player.backpackSlot);
        }

        internal void PrepareEquipped(BackpackSlot slot)
        {
            if (slot == null || slot.data == null) return;
            BackpackData data;
            if (!slot.data.TryGetDataEntry<BackpackData>(DataEntryKey.BackpackData, out data) || data == null) return;
            int expanded = ExpandedCapacity(slot.backpackType);
            int native = NativeCapacity(slot.backpackType);
            if (expanded <= 0) return;
            if (Enabled)
            {
                EnsureCapacity(data, expanded);
                _status = "Active — " + DisplayName(slot.backpackType) + " has " + expanded + " slots.";
            }
            else if (RestoreNativeCapacity(data, slot.backpackType)) _status = "Off";
            else _status = "Off — empty slots " + native + "–" + (data.itemSlots.Length - 1) + " before capacity can return to normal.";
        }

        internal void PrepareBackpackItem(Backpack backpack)
        {
            if (!Enabled || backpack == null) return;
            int expanded = ExpandedCapacity(backpack.backpackType);
            if (expanded <= 0) return;
            try
            {
                BackpackData data = backpack.GetData<BackpackData>(DataEntryKey.BackpackData);
                EnsureCapacity(data, expanded);
            }
            catch (Exception ex) { LogFailure("backpack item capacity", ex); }
        }

        internal bool PrepareWheel(BackpackReference reference, ref int requestedSlots, BackpackSlot.BackpackType type, BackpackWheel wheel)
        {
            int expanded = ExpandedCapacity(type);
            if (expanded <= 0) return true;
            try
            {
                BackpackData data = reference.GetData();
                if (data == null) return true;
                Array slices = wheel == null || WheelSlicesField == null ? null : WheelSlicesField.GetValue(wheel) as Array;
                int wheelSlots = slices == null ? 0 : Math.Max(0, slices.Length - 1);
                if (Enabled)
                {
                    if (wheelSlots < expanded)
                    {
                        _status = "Bigger Backpack disabled for this UI: PEAK exposed only " + wheelSlots + " radial slots.";
                        CloseUnsafeWheel(wheel);
                        return false;
                    }
                    EnsureCapacity(data, expanded);
                    requestedSlots = expanded;
                    return true;
                }
                int native = NativeCapacity(type);
                if (!RestoreNativeCapacity(data, type))
                {
                    if (wheelSlots < data.itemSlots.Length)
                    {
                        _status = "Empty the occupied extra slots before opening this pack with PEAK's current wheel.";
                        CloseUnsafeWheel(wheel);
                        return false;
                    }
                    requestedSlots = data.itemSlots.Length;
                }
                return true;
            }
            catch (Exception ex)
            {
                LogFailure("backpack wheel capacity", ex);
                CloseUnsafeWheel(wheel);
                return false;
            }
        }

        private static void CloseUnsafeWheel(BackpackWheel wheel)
        {
            if (wheel != null) wheel.gameObject.SetActive(false);
            Character local = Character.localCharacter;
            if (local != null && local.data != null) local.data.usingBackpackWheel = false;
        }

        public void ResetScene()
        {
            TryRestoreLocalCapacity();
            _status = Enabled ? "Waiting for an equipped Backpack or Fanny Pack." : "Off";
        }

        private void TryRestoreLocalCapacity()
        {
            Character character = Character.localCharacter;
            if (character == null || character.player == null) return;
            PrepareEquipped(character.player.backpackSlot);
        }

        internal static void PrepareIncoming(BackpackData data, ref InventorySyncData incoming)
        {
            if (data == null || incoming.slots == null) return;
            int capacity = Math.Min(BackpackSlots, incoming.slots.Length);
            if (capacity > NativeBackpackSlots) EnsureCapacity(data, capacity);
        }

        internal static void EnsureCapacity(BackpackData data, int capacity)
        {
            if (data == null) return;
            capacity = Math.Max(0, Math.Min(BackpackSlots, capacity));
            ItemSlot[] current = data.itemSlots;
            if (current != null && current.Length >= capacity) return;
            ItemSlot[] expanded = new ItemSlot[capacity];
            int copy = current == null ? 0 : Math.Min(current.Length, expanded.Length);
            for (int i = 0; i < copy; i++) expanded[i] = current[i];
            for (int i = 0; i < expanded.Length; i++) if (expanded[i] == null) expanded[i] = new ItemSlot((byte)i);
            data.itemSlots = expanded;
        }

        private static bool TryShrink(BackpackData data, int capacity)
        {
            if (data == null || data.itemSlots == null || data.itemSlots.Length <= capacity) return true;
            for (int i = capacity; i < data.itemSlots.Length; i++) if (data.itemSlots[i] != null && !data.itemSlots[i].IsEmpty()) return false;
            ItemSlot[] restored = new ItemSlot[capacity];
            Array.Copy(data.itemSlots, restored, capacity);
            for (int i = 0; i < restored.Length; i++) if (restored[i] == null) restored[i] = new ItemSlot((byte)i);
            data.itemSlots = restored;
            return true;
        }

        private static bool RestoreNativeCapacity(BackpackData data, BackpackSlot.BackpackType type)
        {
            int visibleCapacity = NativeCapacity(type);
            if (data == null || data.itemSlots == null) return true;
            for (int i = visibleCapacity; i < data.itemSlots.Length; i++) if (data.itemSlots[i] != null && !data.itemSlots[i].IsEmpty()) return false;
            // PEAK's Fanny Pack normally keeps the four-entry BackpackData array and limits only the wheel to two slots.
            return type == BackpackSlot.BackpackType.Fannypack || TryShrink(data, NativeBackpackSlots);
        }

        private static int ExpandedCapacity(BackpackSlot.BackpackType type)
        {
            if (type == BackpackSlot.BackpackType.Backpack) return BackpackSlots;
            if (type == BackpackSlot.BackpackType.Fannypack) return FannyPackSlots;
            return 0;
        }

        private static int NativeCapacity(BackpackSlot.BackpackType type)
        {
            return type == BackpackSlot.BackpackType.Fannypack ? NativeFannyPackSlots : NativeBackpackSlots;
        }

        private static string DisplayName(BackpackSlot.BackpackType type)
        {
            return type == BackpackSlot.BackpackType.Fannypack ? "Fanny Pack" : "Backpack";
        }

        private void LogFailure(string operation, Exception ex)
        {
            _status = "Bigger Backpack skipped an incompatible PEAK path safely.";
            if (UnityEngine.Time.unscaledTime < _nextErrorLog) return;
            _nextErrorLog = UnityEngine.Time.unscaledTime + 10f;
            _log.LogWarning("[PTM] Bigger Backpack " + operation + " failed safely: " + ex.Message);
        }
    }

    [HarmonyPatch(typeof(BackpackWheel), "InitWheel")]
    internal static class BiggerBackpackWheelPatch
    {
        private static bool Prefix(BackpackWheel __instance, BackpackReference __0, ref int __1, BackpackSlot.BackpackType __2)
        {
            TrollModPlugin plugin = TrollModPlugin.Instance;
            return plugin == null || plugin.BiggerBackpack == null || plugin.BiggerBackpack.PrepareWheel(__0, ref __1, __2, __instance);
        }
    }

    [HarmonyPatch(typeof(Backpack), "HasSpace")]
    internal static class BiggerBackpackItemCapacityPatch
    {
        private static void Prefix(Backpack __instance)
        {
            TrollModPlugin plugin = TrollModPlugin.Instance;
            if (plugin != null && plugin.BiggerBackpack != null) plugin.BiggerBackpack.PrepareBackpackItem(__instance);
        }
    }

    [HarmonyPatch(typeof(CharacterBackpackHandler), "StashInBackpack")]
    internal static class BiggerEquippedBackpackCapacityPatch
    {
        private static void Prefix(CharacterBackpackHandler __instance)
        {
            TrollModPlugin plugin = TrollModPlugin.Instance;
            Character local = Character.localCharacter;
            if (plugin != null && plugin.BiggerBackpack != null && local != null && local.refs != null && local.refs.backpackHandler == __instance && local.player != null) plugin.BiggerBackpack.PrepareEquipped(local.player.backpackSlot);
        }
    }

    [HarmonyPatch(typeof(BackpackData), "DeserializeValue")]
    internal static class BiggerBackpackDeserializePatch
    {
        private static readonly MethodInfo DeserializeMethod = AccessTools.Method(typeof(InventorySyncData), "Deserialize");
        private static readonly MethodInfo PrepareMethod = AccessTools.Method(typeof(BiggerBackpackManager), "PrepareIncoming");
        private static readonly FieldInfo ItemSlotsField = AccessTools.Field(typeof(BackpackData), "itemSlots");

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> source = new List<CodeInstruction>(instructions);
            bool inserted = false;
            bool replacedLimit = false;
            for (int i = 0; i < source.Count; i++)
            {
                CodeInstruction instruction = source[i];
                if (inserted && !replacedLimit && instruction.opcode == OpCodes.Ldc_I4_4)
                {
                    CodeInstruction loadThis = new CodeInstruction(OpCodes.Ldarg_0);
                    loadThis.labels.AddRange(instruction.labels);
                    loadThis.blocks.AddRange(instruction.blocks);
                    yield return loadThis;
                    yield return new CodeInstruction(OpCodes.Ldfld, ItemSlotsField);
                    yield return new CodeInstruction(OpCodes.Ldlen);
                    yield return new CodeInstruction(OpCodes.Conv_I4);
                    replacedLimit = true;
                    continue;
                }

                yield return instruction;
                if (!inserted && instruction.Calls(DeserializeMethod))
                {
                    object local = i >= 2 ? source[i - 2].operand : null;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, local);
                    yield return new CodeInstruction(OpCodes.Call, PrepareMethod);
                    inserted = true;
                }
            }
            if (!inserted || !replacedLimit) throw new InvalidOperationException("BackpackData.DeserializeValue no longer matches the verified PEAK slot loop.");
        }
    }
}
