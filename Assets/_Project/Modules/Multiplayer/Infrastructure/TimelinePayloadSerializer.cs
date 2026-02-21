using System;
using System.Collections.Generic;
using System.Text;

public static class TimelinePayloadSerializer
{
    public static string SerializeLoot(IReadOnlyList<LootItemSnapshot> loot)
    {
        if (loot == null || loot.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(256);
        for (int i = 0; i < loot.Count; i++)
        {
            LootItemSnapshot item = loot[i];
            if (string.IsNullOrWhiteSpace(item.ItemId))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(';');
            }

            builder.Append(item.ItemId.Replace(";", string.Empty).Replace(",", string.Empty));
            builder.Append(',');
            builder.Append(Math.Max(1, item.Count));
        }

        return builder.ToString();
    }

    public static IReadOnlyList<LootItemSnapshot> DeserializeLoot(string payload)
    {
        var result = new List<LootItemSnapshot>();
        if (string.IsNullOrWhiteSpace(payload))
        {
            return result;
        }

        string[] records = payload.Split(';');
        for (int i = 0; i < records.Length; i++)
        {
            string record = records[i];
            if (string.IsNullOrWhiteSpace(record))
            {
                continue;
            }

            string[] parts = record.Split(',');
            if (parts.Length < 2)
            {
                continue;
            }

            string itemId = parts[0].Trim();
            if (string.IsNullOrWhiteSpace(itemId))
            {
                continue;
            }

            int count = 1;
            if (!int.TryParse(parts[1], out count))
            {
                count = 1;
            }

            result.Add(new LootItemSnapshot(itemId, count));
        }

        return result;
    }

    public static string SerializeInventory(CharacterInventorySnapshot snapshot)
    {
        if (snapshot.Slots == null || snapshot.Slots.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(512);
        for (int i = 0; i < snapshot.Slots.Count; i++)
        {
            InventorySlotSnapshot slot = snapshot.Slots[i];
            if (slot.SlotIndex < 0)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(';');
            }

            builder.Append(slot.SlotIndex);
            builder.Append(',');
            builder.Append((slot.ItemId ?? string.Empty).Replace(";", string.Empty).Replace(",", string.Empty));
            builder.Append(',');
            builder.Append(Math.Max(0, slot.Count));
        }

        return builder.ToString();
    }

    public static CharacterInventorySnapshot DeserializeInventory(int actorId, string payload)
    {
        var slots = new List<InventorySlotSnapshot>();
        if (string.IsNullOrWhiteSpace(payload))
        {
            return new CharacterInventorySnapshot(actorId, slots);
        }

        string[] records = payload.Split(';');
        for (int i = 0; i < records.Length; i++)
        {
            string record = records[i];
            if (string.IsNullOrWhiteSpace(record))
            {
                continue;
            }

            string[] parts = record.Split(',');
            if (parts.Length < 3)
            {
                continue;
            }

            if (!int.TryParse(parts[0], out int slotIndex))
            {
                continue;
            }

            string itemId = parts[1].Trim();
            if (!int.TryParse(parts[2], out int count))
            {
                count = 0;
            }

            slots.Add(new InventorySlotSnapshot(slotIndex, itemId, count));
        }

        return new CharacterInventorySnapshot(actorId, slots);
    }
}
