using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameSceneNames
{
    public const string MainMenuScene = "MainMenuScene";
    public const string LobbyScene = "LobbyScene";
    public const string GameScene = "GameScene";
    public const string FinishScene = "FinishScene";
}

public static class GameSessionState
{
    public const int MaxSlots = 4;

    private static readonly LobbyClassOption[] ClassOptions =
    {
        new("samurai", "Самурай"),
        new("runner", "Бегун"),
        new("brute", "Громила"),
        new("jester", "Шут"),
        new("savage", "Дикарь"),
        new("traveler", "Путник")
    };

    private static readonly LobbyCharacterSlot[] Slots = CreateEmptySlots();

    public static string WinnerName { get; private set; } = string.Empty;

    public static IReadOnlyList<LobbyClassOption> AvailableClasses => ClassOptions;

    public static void ResetLobby()
    {
        for (int i = 0; i < Slots.Length; i++)
        {
            Slots[i].IsEnabled = false;
            Slots[i].Name = string.Empty;
            Slots[i].ClassId = ClassOptions[0].Id;
        }
    }

    public static LobbyCharacterSlot GetSlot(int index)
    {
        return Slots[ClampSlotIndex(index)];
    }

    public static bool HasReadyCharacters()
    {
        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i].IsEnabled)
            {
                return true;
            }
        }

        return false;
    }

    public static List<LobbyCharacterSlot> GetReadySlots()
    {
        var result = new List<LobbyCharacterSlot>(Slots.Length);
        for (int i = 0; i < Slots.Length; i++)
        {
            LobbyCharacterSlot slot = Slots[i];
            if (!slot.IsEnabled)
            {
                continue;
            }

            result.Add(new LobbyCharacterSlot
            {
                IsEnabled = true,
                Name = ResolveSlotName(slot.Name, slot.ClassId),
                ClassId = slot.ClassId
            });
        }

        return result;
    }

    public static void SetSlot(int index, bool isEnabled, string name, string classId)
    {
        LobbyCharacterSlot slot = Slots[ClampSlotIndex(index)];
        slot.IsEnabled = isEnabled;
        slot.Name = isEnabled ? (name ?? string.Empty) : string.Empty;
        slot.ClassId = NormalizeClassId(classId);
    }

    public static void ClearSlot(int index)
    {
        SetSlot(index, false, string.Empty, ClassOptions[0].Id);
    }

    public static int GetClassIndex(string classId)
    {
        string normalizedClassId = NormalizeClassId(classId);
        for (int i = 0; i < ClassOptions.Length; i++)
        {
            if (string.Equals(ClassOptions[i].Id, normalizedClassId, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return 0;
    }

    public static string GetClassLabel(string classId)
    {
        return ClassOptions[GetClassIndex(classId)].Label;
    }

    public static CharacterClass CreateCharacterClass(string classId)
    {
        switch (NormalizeClassId(classId))
        {
            case "samurai":
                return new SamuraiClass();
            case "runner":
                return new RunnerClass();
            case "brute":
                return new BruteClass();
            case "jester":
                return new JesterClass();
            case "savage":
                return new SavageClass();
            default:
                return new TravelerClass();
        }
    }

    public static string GetClassIdByIndex(int classIndex)
    {
        int safeIndex = Mathf.Clamp(classIndex, 0, ClassOptions.Length - 1);
        return ClassOptions[safeIndex].Id;
    }

    public static void SetWinner(string winnerName)
    {
        WinnerName = string.IsNullOrWhiteSpace(winnerName) ? "Неизвестный герой" : winnerName.Trim();
    }

    public static void ClearWinner()
    {
        WinnerName = string.Empty;
    }

    private static LobbyCharacterSlot[] CreateEmptySlots()
    {
        var slots = new LobbyCharacterSlot[MaxSlots];
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = new LobbyCharacterSlot
            {
                IsEnabled = false,
                Name = string.Empty,
                ClassId = ClassOptions[0].Id
            };
        }

        return slots;
    }

    private static int ClampSlotIndex(int index)
    {
        return Mathf.Clamp(index, 0, Slots.Length - 1);
    }

    private static string NormalizeClassId(string classId)
    {
        if (string.IsNullOrWhiteSpace(classId))
        {
            return ClassOptions[0].Id;
        }

        for (int i = 0; i < ClassOptions.Length; i++)
        {
            if (string.Equals(ClassOptions[i].Id, classId.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return ClassOptions[i].Id;
            }
        }

        return ClassOptions[0].Id;
    }

    private static string ResolveSlotName(string name, string classId)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name.Trim();
        }

        return GetClassLabel(classId);
    }
}

[Serializable]
public sealed class LobbyCharacterSlot
{
    public bool IsEnabled;
    public string Name;
    public string ClassId;

    public bool IsConfigured => IsEnabled;
}

[Serializable]
public readonly struct LobbyClassOption
{
    public readonly string Id;
    public readonly string Label;

    public LobbyClassOption(string id, string label)
    {
        Id = id;
        Label = label;
    }
}
