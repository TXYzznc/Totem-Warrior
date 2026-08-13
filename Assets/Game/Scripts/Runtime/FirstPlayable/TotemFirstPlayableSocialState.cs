using System;
using System.Collections.Generic;
using UnityEngine;

public static class TotemPigmentCommandCodec
{
    private const int PigmentMask = 0xFF;
    private const int AmountShift = 8;
    public const int MaxEncodableAmount = 0x7FFFFF;

    public static int EncodeRequest(TotemPigmentKind pigment, int amount) =>
        amount > 0 && amount <= MaxEncodableAmount
            ? ((amount << AmountShift) | ((int)pigment & PigmentMask))
            : 0;

    public static bool TryDecodeRequest(
        in TotemGameplayCommand command,
        out TotemPigmentKind pigment,
        out int amount)
    {
        pigment = (TotemPigmentKind)(command.IntValue & PigmentMask);
        amount = command.IntValue >> AmountShift;
        return command.IsValid
            && command.Type == TotemGameplayCommandType.RequestPigment
            && amount > 0
            && IsValidPigment(pigment);
    }

    public static int EncodeResolution(int requestId, bool approve) =>
        approve ? Mathf.Max(0, requestId) : -Mathf.Max(0, requestId);

    public static bool TryDecodeResolution(
        in TotemGameplayCommand command,
        out int requestId,
        out bool approve)
    {
        approve = command.IntValue > 0;
        requestId = command.IntValue == int.MinValue ? 0 : Math.Abs(command.IntValue);
        return command.IsValid
            && command.Type == TotemGameplayCommandType.ResolvePigmentRequest
            && requestId > 0;
    }

    private static bool IsValidPigment(TotemPigmentKind pigment) =>
        pigment == TotemPigmentKind.Fire
        || pigment == TotemPigmentKind.Ice
        || pigment == TotemPigmentKind.Lightning;
}

public class TotemMatchAchievementCounter
{
    private TotemMatchAchievementSnapshot value;

    public TotemMatchAchievementSnapshot Capture() => value;

    public void AddPlayerDamage(float amount) => value.playerDamage += Positive(amount);
    public void AddPlayerDown() => value.playerDowns++;
    public void AddPlayerElimination() => value.playerEliminations++;
    public void AddAllyHealing(float amount) => value.allyHealing += Positive(amount);
    public void AddAllyShieldOrMitigation(float amount) => value.allyShieldOrMitigation += Positive(amount);
    public void AddSuccessfulRevive() => value.successfulRevives++;
    public void AddCleanseOrControlRemoval() => value.cleansesOrControlRemovals++;

    public void AddEffectiveControl(float seconds)
    {
        float clamped = Positive(seconds);
        if (clamped <= 0f)
        {
            return;
        }

        value.effectiveControlSeconds += clamped;
        value.effectiveControlCount++;
    }

    public void AddAllyDamageGainCreated(float amount) => value.allyDamageGainCreated += Positive(amount);
    public void AddResourcesAcquired(int amount) => value.resourcesAcquired += Math.Max(0, amount);
    public void AddResourcesShared(int amount) => value.resourcesShared += Math.Max(0, amount);
    public void AddSelfDown() => value.selfDowns++;
    public void AddIndirectElementDamage(float amount) => value.indirectElementDamage += Positive(amount);

    private static float Positive(float valueToClamp) => Mathf.Max(0f, valueToClamp);
}

public sealed class TotemPigmentTradeLedger
{
    private sealed class Record
    {
        public TotemPigmentRequest Request;
        public int CreatedPhase;
    }

    private readonly Dictionary<int, Record> records = new Dictionary<int, Record>();
    private int nextRequestId = 1;

    public int Count => records.Count;

    public bool TryCreate(
        TotemParticipantId requesterId,
        TotemParticipantId teammateId,
        TotemPigmentKind pigment,
        int amount,
        int createdSequence,
        int createdPhase,
        TotemFirstPlayableTattooBuildState teammateInventory,
        out TotemPigmentRequest request)
    {
        if (!requesterId.IsValid
            || !teammateId.IsValid
            || requesterId == teammateId
            || amount <= 0
            || teammateInventory == null
            || teammateInventory.GetPigment(pigment) < amount)
        {
            request = default;
            return false;
        }

        int requestId = nextRequestId++;
        request = new TotemPigmentRequest(
            requestId,
            requesterId,
            teammateId,
            pigment,
            amount,
            createdSequence,
            TotemPigmentRequestState.Pending);
        records.Add(requestId, new Record { Request = request, CreatedPhase = createdPhase });
        return true;
    }

    public bool TryResolve(
        int requestId,
        TotemParticipantId responderId,
        bool approve,
        TotemFirstPlayableTattooBuildState donorInventory,
        TotemFirstPlayableTattooBuildState receiverInventory,
        out TotemPigmentRequest resolvedRequest,
        out TotemPigmentTransfer transfer)
    {
        transfer = default;
        if (!records.TryGetValue(requestId, out Record record)
            || record.Request.State != TotemPigmentRequestState.Pending
            || responderId != record.Request.TeammateId)
        {
            resolvedRequest = default;
            return false;
        }

        if (!approve)
        {
            resolvedRequest = WithState(record.Request, TotemPigmentRequestState.Rejected);
            record.Request = resolvedRequest;
            return true;
        }

        if (donorInventory == null
            || receiverInventory == null
            || !donorInventory.TryTransferPigmentTo(
                receiverInventory,
                record.Request.Pigment,
                record.Request.Amount,
                out int inventoryVersion))
        {
            resolvedRequest = WithState(record.Request, TotemPigmentRequestState.Invalidated);
            record.Request = resolvedRequest;
            return false;
        }

        resolvedRequest = WithState(record.Request, TotemPigmentRequestState.Approved);
        record.Request = resolvedRequest;
        transfer = new TotemPigmentTransfer(
            requestId,
            record.Request.TeammateId,
            record.Request.RequesterId,
            record.Request.Pigment,
            record.Request.Amount,
            inventoryVersion);
        return true;
    }

    public int ExpirePendingExceptPhase(int activePhase)
    {
        int expired = 0;
        foreach (Record record in records.Values)
        {
            if (record.Request.State != TotemPigmentRequestState.Pending
                || record.CreatedPhase == activePhase)
            {
                continue;
            }

            record.Request = WithState(record.Request, TotemPigmentRequestState.Expired);
            expired++;
        }

        return expired;
    }

    public bool TryGet(int requestId, out TotemPigmentRequest request)
    {
        if (records.TryGetValue(requestId, out Record record))
        {
            request = record.Request;
            return true;
        }

        request = default;
        return false;
    }

    public TotemPigmentRequest[] CaptureAll()
    {
        var result = new TotemPigmentRequest[records.Count];
        int index = 0;
        foreach (Record record in records.Values)
        {
            result[index++] = record.Request;
        }

        Array.Sort(result, (left, right) => left.RequestId.CompareTo(right.RequestId));
        return result;
    }

    public void Reset()
    {
        records.Clear();
        nextRequestId = 1;
    }

    private static TotemPigmentRequest WithState(
        in TotemPigmentRequest request,
        TotemPigmentRequestState state)
    {
        return new TotemPigmentRequest(
            request.RequestId,
            request.RequesterId,
            request.TeammateId,
            request.Pigment,
            request.Amount,
            request.CreatedSequence,
            state);
    }
}
