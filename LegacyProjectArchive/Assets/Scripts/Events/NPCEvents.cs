using System.Collections.Generic;
using Economy;
using Tattoo.Data;
using UnityEngine;

/// <summary>NPC 系统事件（CONTRACT §1.5 + v2.1 追加）。</summary>

// ===== 通用 NPC 引用 =====

/// <summary>轻量 NPC 标识，避免传递整个 NPCInstance 引用（struct 值语义）。</summary>
public struct NPCRef
{
    public int    ConfigId;   // NPCConfig.Id
    public string NPCId;     // NPCConfig.NPCId 字符串
    public NPCType Type;
    public Vector3 Position;
}

public enum NPCType { Tattooist, Merchant }

// ===== 发布事件 =====

/// <summary>玩家或 Bot 开始与 NPC 交互（弹窗打开前）。</summary>
public class NPCInteractStartEvent
{
    public Actor   Interactor;
    public NPCRef  Npc;

    public NPCInteractStartEvent(Actor interactor, NPCRef npc)
    {
        Interactor = interactor;
        Npc        = npc;
    }
}

// ShopPurchaseEvent 定义在 Economy.Events（Assets/Scripts/Modules/Economy/EconomyEvents.cs），
// NPCModule 直接使用 Economy.Events.ShopPurchaseEvent，此处不重复定义。

/// <summary>商人库存刷新（InitializeAsync 初始抽取 + 手动刷新各触发一次）。
/// CONTRACT §1.5。</summary>
public class ShopRefreshEvent
{
    public NPCRef     Shop;
    public List<int>  NewStock;

    public ShopRefreshEvent(NPCRef shop, List<int> newStock)
    {
        Shop     = shop;
        NewStock = newStock;
    }
}

/// <summary>内部事件：附魔会话被取消（超时 / ShutdownAsync 中断）。
/// 仅供 UIModule 关闭进度条弹窗，不进 CONTRACT。</summary>
public class EnchantSessionCancelledEvent
{
    public Actor  Owner;
    public string Reason;

    public EnchantSessionCancelledEvent(Actor owner, string reason)
    {
        Owner  = owner;
        Reason = reason;
    }
}

// ===== 订阅事件（由 InputModule / BotControllerModule 发出）=====

/// <summary>玩家按下交互键（E / 手柄 A）。InputModule 发出。</summary>
public class InteractPressedEvent
{
    public Actor Interactor;

    public InteractPressedEvent(Actor interactor)
    {
        Interactor = interactor;
    }
}

/// <summary>Bot 主动触发 NPC 交互请求。BotControllerModule 发出。</summary>
public class BotInteractRequestEvent
{
    public Actor  Bot;
    /// <summary>目标 NPCConfig.Id（-1 = 最近可用）。</summary>
    public int    TargetNpcId;

    public BotInteractRequestEvent(Actor bot, int targetNpcId = -1)
    {
        Bot         = bot;
        TargetNpcId = targetNpcId;
    }
}

/// <summary>UI 确认附魔（UIModule → NPCModule 回调）。</summary>
public class UIEnchantConfirmEvent
{
    public Actor Interactor;
    public int   PartSlotIndex;

    public UIEnchantConfirmEvent(Actor interactor, int partSlotIndex)
    {
        Interactor    = interactor;
        PartSlotIndex = partSlotIndex;
    }
}

/// <summary>UI 确认购买（UIModule → NPCModule 回调）。</summary>
public class UIShopBuyConfirmEvent
{
    public Actor Buyer;
    public int   NpcConfigId;
    public int   ItemId;

    public UIShopBuyConfirmEvent(Actor buyer, int npcConfigId, int itemId)
    {
        Buyer       = buyer;
        NpcConfigId = npcConfigId;
        ItemId      = itemId;
    }
}
