using System;

public enum TotemTattooMutationCode : byte
{
    Applied = 0,
    NotBuildPhase = 1,
    InvalidSlot = 2,
    InvalidPattern = 3,
    InvalidElement = 4,
    InsufficientPigment = 5,
    EmptySlot = 6,
    InvalidCommand = 7,
}

public static class TotemFirstPlayableTattooCommandCodec
{
    private const int SlotMask = 0x07;
    private const int PatternShift = 3;
    private const int PatternMask = 0x03;
    private const int ElementShift = 5;
    private const int ElementMask = 0x03;

    public static int EncodeEquip(
        TotemTattooSlotId slot,
        TotemFirstPlayablePatternId pattern,
        TotemFirstPlayableElement element)
    {
        return ((int)slot & SlotMask)
            | (((int)pattern & PatternMask) << PatternShift)
            | (((int)element & ElementMask) << ElementShift);
    }

    public static bool TryDecodeEquip(
        in TotemGameplayCommand command,
        out TotemTattooSlotId slot,
        out TotemFirstPlayablePatternId pattern,
        out TotemFirstPlayableElement element)
    {
        slot = (TotemTattooSlotId)(command.IntValue & SlotMask);
        pattern = (TotemFirstPlayablePatternId)((command.IntValue >> PatternShift) & PatternMask);
        element = (TotemFirstPlayableElement)((command.IntValue >> ElementShift) & ElementMask);
        return command.IsValid
            && command.Type == TotemGameplayCommandType.EquipTattoo
            && (int)slot < TotemFirstPlayableTattooBuildState.SlotCount
            && TotemFirstPlayableTattooBuildState.IsAvailablePattern(pattern)
            && element != TotemFirstPlayableElement.None
            && element <= TotemFirstPlayableElement.Lightning;
    }

    public static int EncodeRemove(TotemTattooSlotId slot) => (int)slot;

    public static bool TryDecodeRemove(in TotemGameplayCommand command, out TotemTattooSlotId slot)
    {
        slot = (TotemTattooSlotId)command.IntValue;
        return command.IsValid
            && command.Type == TotemGameplayCommandType.RemoveTattoo
            && command.IntValue >= 0
            && command.IntValue < TotemFirstPlayableTattooBuildState.SlotCount;
    }
}

public readonly struct TotemTattooLoadoutEntry
{
    public TotemTattooLoadoutEntry(
        TotemTattooSlotId slot,
        TotemFirstPlayablePatternId pattern,
        TotemFirstPlayableElement element)
    {
        Slot = slot;
        Pattern = pattern;
        Element = element;
    }

    public TotemTattooSlotId Slot { get; }
    public TotemFirstPlayablePatternId Pattern { get; }
    public TotemFirstPlayableElement Element { get; }
    public bool IsEquipped => Pattern != TotemFirstPlayablePatternId.None && Element != TotemFirstPlayableElement.None;
}

public readonly struct TotemTattooMutationResult
{
    public TotemTattooMutationResult(
        TotemTattooMutationCode code,
        TotemTattooLoadoutEntry previous,
        TotemTattooLoadoutEntry current,
        TotemPigmentKind spentPigment,
        int spentAmount,
        TotemPigmentKind refundedPigment,
        int refundedAmount,
        int inventoryVersion)
    {
        Code = code;
        Previous = previous;
        Current = current;
        SpentPigment = spentPigment;
        SpentAmount = spentAmount;
        RefundedPigment = refundedPigment;
        RefundedAmount = refundedAmount;
        InventoryVersion = inventoryVersion;
    }

    public TotemTattooMutationCode Code { get; }
    public TotemTattooLoadoutEntry Previous { get; }
    public TotemTattooLoadoutEntry Current { get; }
    public TotemPigmentKind SpentPigment { get; }
    public int SpentAmount { get; }
    public TotemPigmentKind RefundedPigment { get; }
    public int RefundedAmount { get; }
    public int InventoryVersion { get; }
    public bool Applied => Code == TotemTattooMutationCode.Applied;
}

public readonly struct TotemBotTattooPlan
{
    public TotemBotTattooPlan(
        TotemTattooSlotId slot,
        TotemFirstPlayablePatternId pattern,
        TotemFirstPlayableElement element)
    {
        Slot = slot;
        Pattern = pattern;
        Element = element;
    }

    public TotemTattooSlotId Slot { get; }
    public TotemFirstPlayablePatternId Pattern { get; }
    public TotemFirstPlayableElement Element { get; }
    public bool IsValid => TotemFirstPlayableTattooBuildState.IsAvailablePattern(Pattern)
                           && Element != TotemFirstPlayableElement.None;
}

public static class TotemFirstPlayableBotBuildPlanner
{
    public static bool TryPlan(
        int participantId,
        int buildOrdinal,
        TotemFirstPlayableTattooBuildState state,
        out TotemBotTattooPlan plan)
    {
        plan = default;
        if (participantId <= 0 || buildOrdinal < 0 || state == null)
        {
            return false;
        }

        TotemTattooLoadoutEntry previous = state.GetSlot(TotemTattooSlotId.RightArm);
        TotemFirstPlayablePatternId pattern = ((participantId + buildOrdinal) & 1) == 0
            ? TotemFirstPlayablePatternId.P01
            : TotemFirstPlayablePatternId.P02;
        for (int offset = 0; offset < 3; offset++)
        {
            int elementIndex = (participantId + buildOrdinal + offset) % 3;
            TotemFirstPlayableElement element = (TotemFirstPlayableElement)(elementIndex + 1);
            int available = state.GetPigment((TotemPigmentKind)element);
            if (previous.IsEquipped && previous.Element == element)
            {
                available += TotemFirstPlayableTattooBuildState.RemovePigmentRefund;
            }

            if (available < TotemFirstPlayableTattooBuildState.EquipPigmentCost
                || (previous.IsEquipped && previous.Pattern == pattern && previous.Element == element))
            {
                continue;
            }

            plan = new TotemBotTattooPlan(TotemTattooSlotId.RightArm, pattern, element);
            return true;
        }

        return false;
    }
}

public sealed class TotemFirstPlayableTattooBuildState
{
    public const int SlotCount = 6;
    public const int EquipPigmentCost = 10;
    public const int RemovePigmentRefund = 6;

    private readonly TotemTattooLoadoutEntry[] slots = new TotemTattooLoadoutEntry[SlotCount];
    private int firePigment;
    private int icePigment;
    private int lightningPigment;
    private int inventoryVersion;

    public TotemFirstPlayableTattooBuildState()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = Empty((TotemTattooSlotId)i);
        }
    }

    public int InventoryVersion => inventoryVersion;

    public static bool IsBuildPhase(TotemMatchPhase phase)
    {
        return phase == TotemMatchPhase.OpeningBuild
            || phase == TotemMatchPhase.Build2
            || phase == TotemMatchPhase.Build3
            || phase == TotemMatchPhase.Build4
            || phase == TotemMatchPhase.Build5;
    }

    public static bool IsAvailablePattern(TotemFirstPlayablePatternId pattern)
    {
        return pattern == TotemFirstPlayablePatternId.P01 || pattern == TotemFirstPlayablePatternId.P02;
    }

    public static string GetPublicEffectText(TotemFirstPlayablePatternId pattern)
    {
        switch (pattern)
        {
            case TotemFirstPlayablePatternId.P01:
                return TotemFirstPlayableTattooBuildConfig.P01PublicEffectText;
            case TotemFirstPlayablePatternId.P02:
                return TotemFirstPlayableTattooBuildConfig.P02PublicEffectText;
            default:
                return string.Empty;
        }
    }

    public int GetPigment(TotemPigmentKind pigment)
    {
        switch (pigment)
        {
            case TotemPigmentKind.Fire:
                return firePigment;
            case TotemPigmentKind.Ice:
                return icePigment;
            case TotemPigmentKind.Lightning:
                return lightningPigment;
            default:
                return 0;
        }
    }

    public void SetPigment(TotemPigmentKind pigment, int amount)
    {
        if (!IsValidPigment(pigment))
        {
            return;
        }

        SetPigmentInternal(pigment, Math.Max(0, amount));
        inventoryVersion++;
    }

    public void AddPigment(TotemPigmentKind pigment, int amount)
    {
        if (amount <= 0 || !IsValidPigment(pigment))
        {
            return;
        }

        int current = GetPigment(pigment);
        int next = amount > int.MaxValue - current ? int.MaxValue : current + amount;
        SetPigmentInternal(pigment, next);
        inventoryVersion++;
    }

    public bool TryTransferPigmentTo(
        TotemFirstPlayableTattooBuildState receiver,
        TotemPigmentKind pigment,
        int amount,
        out int sourceInventoryVersion)
    {
        sourceInventoryVersion = inventoryVersion;
        if (receiver == null
            || ReferenceEquals(receiver, this)
            || amount <= 0
            || !IsValidPigment(pigment)
            || GetPigment(pigment) < amount)
        {
            return false;
        }

        int receiverCurrent = receiver.GetPigment(pigment);
        if (receiverCurrent > int.MaxValue - amount)
        {
            return false;
        }

        SetPigmentInternal(pigment, GetPigment(pigment) - amount);
        receiver.SetPigmentInternal(pigment, receiverCurrent + amount);
        inventoryVersion++;
        receiver.inventoryVersion++;
        sourceInventoryVersion = inventoryVersion;
        return true;
    }

    public TotemTattooLoadoutEntry GetSlot(TotemTattooSlotId slot)
    {
        return IsValidSlot(slot) ? slots[(int)slot] : default;
    }

    public TotemTattooLoadoutEntry[] CaptureLoadout()
    {
        var result = new TotemTattooLoadoutEntry[slots.Length];
        Array.Copy(slots, result, slots.Length);
        return result;
    }

    public bool ClearForMatchCleanup()
    {
        bool changed = firePigment != 0 || icePigment != 0 || lightningPigment != 0;
        firePigment = 0;
        icePigment = 0;
        lightningPigment = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsEquipped)
            {
                changed = true;
                slots[i] = Empty((TotemTattooSlotId)i);
            }
        }

        if (changed)
        {
            inventoryVersion++;
        }

        return changed;
    }

    public bool TryEquip(
        TotemMatchPhase phase,
        TotemTattooSlotId slot,
        TotemFirstPlayablePatternId pattern,
        TotemFirstPlayableElement element,
        out TotemTattooMutationResult result)
    {
        TotemTattooLoadoutEntry previous = GetSlot(slot);
        if (!IsBuildPhase(phase))
        {
            result = Rejected(TotemTattooMutationCode.NotBuildPhase, previous);
            return false;
        }

        if (!IsValidSlot(slot))
        {
            result = Rejected(TotemTattooMutationCode.InvalidSlot, previous);
            return false;
        }

        if (!IsAvailablePattern(pattern))
        {
            result = Rejected(TotemTattooMutationCode.InvalidPattern, previous);
            return false;
        }

        if (!TryMapPigment(element, out TotemPigmentKind spentPigment))
        {
            result = Rejected(TotemTattooMutationCode.InvalidElement, previous);
            return false;
        }

        TotemPigmentKind refundedPigment = default;
        int refundedAmount = 0;
        int available = GetPigment(spentPigment);
        if (previous.IsEquipped)
        {
            TryMapPigment(previous.Element, out refundedPigment);
            refundedAmount = RemovePigmentRefund;
            if (refundedPigment == spentPigment)
            {
                available += refundedAmount;
            }
        }

        if (available < EquipPigmentCost)
        {
            result = Rejected(TotemTattooMutationCode.InsufficientPigment, previous);
            return false;
        }

        if (refundedAmount > 0)
        {
            SetPigmentInternal(refundedPigment, GetPigment(refundedPigment) + refundedAmount);
        }

        SetPigmentInternal(spentPigment, GetPigment(spentPigment) - EquipPigmentCost);
        var current = new TotemTattooLoadoutEntry(slot, pattern, element);
        slots[(int)slot] = current;
        inventoryVersion++;
        result = new TotemTattooMutationResult(
            TotemTattooMutationCode.Applied,
            previous,
            current,
            spentPigment,
            EquipPigmentCost,
            refundedPigment,
            refundedAmount,
            inventoryVersion);
        return true;
    }

    public bool TryApplyCommand(
        TotemMatchPhase phase,
        in TotemGameplayCommand command,
        out TotemTattooMutationResult result)
    {
        if (TotemFirstPlayableTattooCommandCodec.TryDecodeEquip(
                command,
                out TotemTattooSlotId equipSlot,
                out TotemFirstPlayablePatternId pattern,
                out TotemFirstPlayableElement element))
        {
            return TryEquip(phase, equipSlot, pattern, element, out result);
        }

        if (TotemFirstPlayableTattooCommandCodec.TryDecodeRemove(command, out TotemTattooSlotId removeSlot))
        {
            return TryRemove(phase, removeSlot, out result);
        }

        result = Rejected(TotemTattooMutationCode.InvalidCommand, default);
        return false;
    }

    public bool TryRemove(
        TotemMatchPhase phase,
        TotemTattooSlotId slot,
        out TotemTattooMutationResult result)
    {
        TotemTattooLoadoutEntry previous = GetSlot(slot);
        if (!IsBuildPhase(phase))
        {
            result = Rejected(TotemTattooMutationCode.NotBuildPhase, previous);
            return false;
        }

        if (!IsValidSlot(slot))
        {
            result = Rejected(TotemTattooMutationCode.InvalidSlot, previous);
            return false;
        }

        if (!previous.IsEquipped || !TryMapPigment(previous.Element, out TotemPigmentKind refundedPigment))
        {
            result = Rejected(TotemTattooMutationCode.EmptySlot, previous);
            return false;
        }

        SetPigmentInternal(refundedPigment, GetPigment(refundedPigment) + RemovePigmentRefund);
        TotemTattooLoadoutEntry current = Empty(slot);
        slots[(int)slot] = current;
        inventoryVersion++;
        result = new TotemTattooMutationResult(
            TotemTattooMutationCode.Applied,
            previous,
            current,
            default,
            0,
            refundedPigment,
            RemovePigmentRefund,
            inventoryVersion);
        return true;
    }

    private TotemTattooMutationResult Rejected(TotemTattooMutationCode code, TotemTattooLoadoutEntry previous)
    {
        return new TotemTattooMutationResult(code, previous, previous, default, 0, default, 0, inventoryVersion);
    }

    private static TotemTattooLoadoutEntry Empty(TotemTattooSlotId slot)
    {
        return new TotemTattooLoadoutEntry(slot, TotemFirstPlayablePatternId.None, TotemFirstPlayableElement.None);
    }

    private static bool IsValidSlot(TotemTattooSlotId slot)
    {
        int value = (int)slot;
        return value >= 0 && value < SlotCount;
    }

    private static bool IsValidPigment(TotemPigmentKind pigment)
    {
        return pigment == TotemPigmentKind.Fire
            || pigment == TotemPigmentKind.Ice
            || pigment == TotemPigmentKind.Lightning;
    }

    private static bool TryMapPigment(TotemFirstPlayableElement element, out TotemPigmentKind pigment)
    {
        switch (element)
        {
            case TotemFirstPlayableElement.Fire:
                pigment = TotemPigmentKind.Fire;
                return true;
            case TotemFirstPlayableElement.Ice:
                pigment = TotemPigmentKind.Ice;
                return true;
            case TotemFirstPlayableElement.Lightning:
                pigment = TotemPigmentKind.Lightning;
                return true;
            default:
                pigment = default;
                return false;
        }
    }

    private void SetPigmentInternal(TotemPigmentKind pigment, int amount)
    {
        switch (pigment)
        {
            case TotemPigmentKind.Fire:
                firePigment = amount;
                break;
            case TotemPigmentKind.Ice:
                icePigment = amount;
                break;
            case TotemPigmentKind.Lightning:
                lightningPigment = amount;
                break;
        }
    }
}
