using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;

using UnityEngine.U2D;
using DG.Tweening;
using System;
using UnityEngine.UI;

public static class UIExtension
{
    /// <summary>
    /// 异步加载并设置Sprite
    /// </summary>
    /// <param name="image"></param>
    /// <param name="spriteName"></param>
    public static void SetSprite(this Image image, string spriteName, bool resize = false)
    {
        spriteName = UtilityBuiltin.AssetsPath.GetSpritesPath(spriteName);
        GF.UI.LoadSprite(spriteName, sp =>
        {
            if (sp != null)
            {
                image.sprite = sp;
                if (resize) image.SetNativeSize();
            }
        });
    }
    /// <summary>
    /// 异步加载并设置Texture
    /// </summary>
    /// <param name="rawImage"></param>
    /// <param name="spriteName"></param>
    public static void SetTexture(this RawImage rawImage, string spriteName, bool resize = false)
    {
        spriteName = UtilityBuiltin.AssetsPath.GetTexturePath(spriteName);
        GF.UI.LoadTexture(spriteName, tex =>
        {
            if (tex != null)
            {
                rawImage.texture = tex;
                if (resize) rawImage.SetNativeSize();
            }
        });
    }
    /// <summary>
    /// 判断是否点击在UI上
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="mousePosition"></param>
    /// <returns></returns>
    public static bool IsPointerOverUIObject(this UIComponent uiCom, Vector3 mousePosition)
    {
        return UtilityEx.IsPointerOverUIObject(mousePosition);
    }

    /// <summary>
    /// 世界坐标转换到UI屏幕坐标
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="worldPos">世界坐标点</param>
    /// <returns></returns>
    public static Vector3 PositionWorldToUI(this UIComponent uiCom, Vector3 worldPos, RectTransform targetRect)
    {
        var viewPos = GF.Scene.MainCamera.WorldToViewportPoint(worldPos);
        var uiPos = GF.UICamera.ViewportToScreenPoint(viewPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, uiPos, GF.UICamera, out var localPoint);
        return localPoint;
    }
    /// <summary>
    /// 加载Sprite图集
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="atlasName"></param>
    /// <param name="onSpriteAtlasLoaded"></param>
    public static void LoadSpriteAtlas(this UIComponent uiCom, string atlasName, GameFrameworkAction<SpriteAtlas> onSpriteAtlasLoaded)
    {
        if (GF.Resource.HasAsset(atlasName) == GameFramework.Resource.HasAssetResult.NotExist)
        {
            Log.Warning("LoadSpriteAtlas失败, 资源不存在:{0}", atlasName);
            GFTrace.Warning("UI", "SpriteAtlas.Missing", null, GFTrace.Data("asset", atlasName));
            return;
        }

        GF.Resource.LoadAsset(atlasName, typeof(SpriteAtlas), new GameFramework.Resource.LoadAssetCallbacks((string assetName, object asset, float duration, object userData) =>
        {
            var spAtlas = asset as SpriteAtlas;
            GFTrace.Success("UI", "SpriteAtlas.Load.Success", null, GFTrace.Data("asset", assetName));
            onSpriteAtlasLoaded.Invoke(spAtlas);
        }));
    }
    /// <summary>
    /// 异步加载Sprite
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="spriteName"></param>
    /// <param name="onSpriteLoaded"></param>
    public static void LoadSprite(this UIComponent uiCom, string spriteName, GameFrameworkAction<Sprite> onSpriteLoaded)
    {
        if (GF.Resource.HasAsset(spriteName) == GameFramework.Resource.HasAssetResult.NotExist)
        {
            Log.Warning("UIExtension.SetSprite()失败, 资源不存在:{0}", spriteName);
            GFTrace.Warning("UI", "Sprite.Missing", null, GFTrace.Data("asset", spriteName));
            return;
        }
        GF.Resource.LoadAsset(spriteName, typeof(Sprite), new GameFramework.Resource.LoadAssetCallbacks((string assetName, object asset, float duration, object userData) =>
        {
            Sprite resultSp = asset as Sprite;
            GFTrace.Success("UI", "Sprite.Load.Success", null, GFTrace.Data("asset", assetName));
            onSpriteLoaded.Invoke(resultSp);
        }));
    }
    /// <summary>
    /// 异步加载Texture
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="spriteName"></param>
    /// <param name="onSpriteLoaded"></param>
    public static void LoadTexture(this UIComponent uiCom, string spriteName, GameFrameworkAction<Texture2D> onSpriteLoaded)
    {
        if (GF.Resource.HasAsset(spriteName) == GameFramework.Resource.HasAssetResult.NotExist)
        {
            Log.Warning("UIExtension.LoadTexture()失败, 资源不存在:{0}", spriteName);
            GFTrace.Warning("UI", "Texture.Missing", null, GFTrace.Data("asset", spriteName));
            return;
        }
        GF.Resource.LoadAsset(spriteName, typeof(Texture2D), new GameFramework.Resource.LoadAssetCallbacks((string assetName, object asset, float duration, object userData) =>
        {
            Texture2D resultSp = asset as Texture2D;
            GFTrace.Success("UI", "Texture.Load.Success", null, GFTrace.Data("asset", assetName));
            onSpriteLoaded.Invoke(resultSp);
        }));
    }
    /// <summary>
    /// Destory指定根节点下的所有子节点
    /// </summary>
    /// <param name="ui"></param>
    /// <param name="parent"></param>
    public static void RemoveAllChildren(this UIComponent ui, Transform parent)
    {
        foreach (Transform child in parent)
        {
            GameObject.Destroy(child.gameObject);
        }
    }
    /// <summary>
    /// 显示Toast提示
    /// </summary>
    /// <param name="ui"></param>
    /// <param name="text"></param>
    /// <param name="duration"></param>
    public static void ShowToast(this UIComponent ui, string text, ToastStyle style = ToastStyle.Blue, float duration = 2)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        Log.Info(text);
        GFTrace.Info("UI", "Toast.Log", text, GFTrace.Data("style", style.ToString(), "duration", duration.ToString("F2")));
    }

    /// <summary>
    /// 打开UI界面
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="viewId">UI界面id(传入自动生成的UIViews枚举值)</param>
    /// <param name="parms"></param>
    /// <returns></returns>
    public static int OpenUIForm(this UIComponent uiCom, UIViews viewId, UIParams parms = null)
    {
        var uiTb = GF.DataTable.GetDataTable<UITable>();
        var uiGroupTb = GF.DataTable.GetDataTable<UIGroupTable>();
        int uiId = (int)viewId;
        if (!uiTb.HasDataRow(uiId))
        {
            Log.Error("UITable表不存在id:{0}", uiId);
            GFTrace.Failure("UI", "OpenUIForm.TableRowMissing", null, GFTrace.Data("uiId", uiId.ToString(), "viewId", viewId.ToString()));
            if (parms != null) GF.VariablePool.ClearVariables(parms.Id);
            return -1;
        }
        var uiRow = uiTb.GetDataRow(uiId);
        if (!uiGroupTb.HasDataRow(uiRow.UIGroupId))
        {
            Log.Error("UIGroupTable表不存在id:{0}", uiId);
            GFTrace.Failure("UI", "OpenUIForm.GroupRowMissing", null, GFTrace.Data("uiId", uiId.ToString(), "uiGroupId", uiRow.UIGroupId.ToString()));
            if (parms != null) GF.VariablePool.ClearVariables(parms.Id);
            return -1;
        }
        var uiGroupRow = uiGroupTb.GetDataRow(uiRow.UIGroupId);
        string uiName = UtilityBuiltin.AssetsPath.GetUIFormPath(uiRow.UIPrefab);
        if (uiCom.IsLoadingUIForm(uiName))
        {
            GFTrace.Warning("UI", "OpenUIForm.AlreadyLoading", null, GFTrace.Data("uiId", uiId.ToString(), "asset", uiName));
            if (parms != null) GF.VariablePool.ClearVariables(parms.Id);
            return -1;
        }
        parms ??= UIParams.Create();
        parms.AllowEscapeClose ??= uiRow.EscapeClose;
        parms.SortOrder ??= uiRow.SortOrder + uiGroupRow.Depth;
        int serialId = uiCom.OpenUIForm(uiName, uiGroupRow.Name, uiRow.PauseCoveredUI, parms);
        GFTrace.Info("UI", "OpenUIForm.Begin", null, GFTrace.Data("uiId", uiId.ToString(), "asset", uiName, "group", uiGroupRow.Name, "serialId", serialId.ToString()));
        return serialId;
    }

    /// <summary>
    /// 关闭UI界面(关闭前播放UI界面关闭动画)
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="ui"></param>
    public static void Close(this UIComponent uiCom, UIForm ui)
    {
        Close(uiCom, ui.SerialId);
    }
    /// <summary>
    /// 关闭UI界面(关闭前播放UI界面关闭动画)
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="uiFormId"></param>
    public static void Close(this UIComponent uiCom, int uiFormId)
    {
        if (uiCom.IsLoadingUIForm(uiFormId))
        {
            GF.UI.CloseUIForm(uiFormId);
            return;
        }
        if (!uiCom.HasUIForm(uiFormId))
        {
            return;
        }
        var uiForm = uiCom.GetUIForm(uiFormId);
        UIFormBase logic = uiForm.Logic as UIFormBase;
        logic.CloseWithAnimation();
    }
    /// <summary>
    /// 关闭整个UI组的所有UI界面
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="groupName"></param>
    public static void CloseUIForms(this UIComponent uiCom, string groupName)
    {
        var group = uiCom.GetUIGroup(groupName);
        var all = group.GetAllUIForms();
        foreach (var item in all)
        {
            uiCom.CloseUIForm(item.SerialId);
        }
    }
    /// <summary>
    /// 判断UI界面是否正在加载队列(还没有实体化)
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="view"></param>
    /// <returns></returns>
    public static bool IsLoadingUIForm(this UIComponent uiCom, UIViews view)
    {
        string assetName = uiCom.GetUIFormAssetName(view);
        return uiCom.IsLoadingUIForm(assetName);
    }
    /// <summary>
    /// 是否已经打开UI界面
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="view"></param>
    /// <returns></returns>
    public static bool HasUIForm(this UIComponent uiCom, UIViews view)
    {
        string assetName = uiCom.GetUIFormAssetName(view);
        if (string.IsNullOrEmpty(assetName))
        {
            return false;
        }

        return uiCom.HasUIForm(assetName);
    }
    /// <summary>
    /// 获取UI界面的prefab资源名
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="view"></param>
    /// <returns></returns>
    public static string GetUIFormAssetName(this UIComponent uiCom, UIViews view)
    {
        if (GF.DataTable == null || !GF.DataTable.HasDataTable<UITable>())
        {
            Log.Warning("GetUIFormAssetName is empty.");
            GFTrace.Warning("UI", "GetUIFormAssetName.NoUITable");
            return string.Empty;
        }

        var uiTb = GF.DataTable.GetDataTable<UITable>();
        if (!uiTb.HasDataRow((int)view))
        {
            return string.Empty;
        }
        string uiName = UtilityBuiltin.AssetsPath.GetUIFormPath(uiTb.GetDataRow((int)view).UIPrefab);
        return uiName;
    }
    /// <summary>
    /// 关闭所有打开的某个界面
    /// </summary>
    /// <param name="uiCom"></param>
    /// <param name="view"></param>
    /// <param name="uiGroup"></param>
    public static void CloseUIForms(this UIComponent uiCom, UIViews view, string uiGroup = null)
    {
        string uiAssetName = uiCom.GetUIFormAssetName(view);
        GameFramework.UI.IUIForm[] uIForms;
        if (string.IsNullOrEmpty(uiGroup))
        {
            uIForms = uiCom.GetUIForms(uiAssetName);
        }
        else
        {
            if (!uiCom.HasUIGroup(uiGroup))
            {
                return;
            }
            uIForms = uiCom.GetUIGroup(uiGroup).GetUIForms(uiAssetName);
        }

        foreach (var item in uIForms)
        {
            uiCom.Close(item.SerialId);
        }
    }
    /// <summary>
    /// 刷新所有UI的多语言文本(当语言切换时需调用),用于即时改变多语言文本
    /// </summary>
    /// <param name="uiCom"></param>
    public static void UpdateLocalizationTexts(this UIComponent uiCom)
    {
        //foreach (var item in Resources.FindObjectsOfTypeAll<TMPro.TMP_FontAsset>())
        //{
        //    item.ClearFontAssetData();
        //}
        foreach (UIForm uiForm in uiCom.GetAllLoadedUIForms())
        {
            (uiForm.Logic as UIFormBase).InitLocalization();
        }
        var uiObjectPool = GF.ObjectPool.GetObjectPool(pool => pool.FullName == "GameFramework.UI.UIManager+UIFormInstanceObject.UI Instance Pool");
        if (uiObjectPool != null)
        {
            uiObjectPool.ReleaseAllUnused();
        }
    }
    /// <summary>
    /// 获取当前顶层的UI界面id(排除子界面)
    /// </summary>
    /// <param name="uiCom"></param>
    /// <returns></returns>
    public static int GetTopUIFormId(this UIComponent uiCom)
    {
        var dialogGp = uiCom.GetUIGroup(Const.UIGroup.Dialog.ToString());
        var allUIForms = dialogGp.GetAllUIForms();
        int maxSortOrder = -1;
        int maxOrderIndex = -1;
        for (int i = 0; i < allUIForms.Length; i++)
        {
            var uiBase = (allUIForms[i] as UIForm).Logic as UIFormBase;
            if (uiBase == null || uiBase.Params.IsSubUIForm) continue;

            int curOrder = uiBase.SortOrder;
            if (curOrder >= maxSortOrder)
            {
                maxSortOrder = curOrder;
                maxOrderIndex = i;
            }
        }
        if (maxOrderIndex != -1) return allUIForms[maxOrderIndex].SerialId;

        maxSortOrder = -1;
        maxOrderIndex = -1;
        var uiFormGp = uiCom.GetUIGroup(Const.UIGroup.UIForm.ToString());
        allUIForms = uiFormGp.GetAllUIForms();
        for (int i = 0; i < allUIForms.Length; i++)
        {
            var uiBase = (allUIForms[i] as UIForm).Logic as UIFormBase;
            if (uiBase == null || uiBase.Params.IsSubUIForm) continue;

            int curOrder = uiBase.SortOrder;
            if (curOrder >= maxSortOrder)
            {
                maxSortOrder = curOrder;
                maxOrderIndex = i;
            }
        }
        if (maxOrderIndex != -1) return allUIForms[maxOrderIndex].SerialId;
        return -1;
    }

    /// <summary>
    /// 由外部输入模块在 Back/Escape 动作触发时调用，避免 UI 基类直接轮询 UnityEngine.Input。
    /// </summary>
    /// <param name="uiCom"></param>
    /// <returns>成功关闭顶层 UI 时返回 true。</returns>
    public static bool TryCloseTopBackUIForm(this UIComponent uiCom)
    {
        int uiFormId = uiCom.GetTopUIFormId();
        if (uiFormId < 0 || !uiCom.HasUIForm(uiFormId))
        {
            return false;
        }

        var uiForm = uiCom.GetUIForm(uiFormId);
        var logic = uiForm.Logic as UIFormBase;
        if (logic == null || !logic.CanCloseByInputModule)
        {
            return false;
        }

        bool closed = logic.TryCloseFromInputModule();
        if (closed)
        {
            GFTrace.Info("UI", "InputModule.CloseTopBackUIForm", null, GFTrace.Data("serialId", uiFormId.ToString()));
        }

        return closed;
    }

    public static bool TryCloseTopEscapeUIForm(this UIComponent uiCom)
    {
        return uiCom.TryCloseTopBackUIForm();
    }
    public static void ShowRewardEffect(this UIComponent uiCom, Vector3 centerPos, Vector3 fly2Pos, float flyDelay = 0.5f, GameFrameworkAction onAnimComplete = null, int num = 30)
    {
        Log.Warning("ShowRewardEffect is not configured in the clean workspace.");
        onAnimComplete?.Invoke();
    }


    #region Unity UI Extension
    public static void SetAnchoredPositionX(this RectTransform rectTransform, float anchoredPositionX)
    {
        var value = rectTransform.anchoredPosition;
        value.x = anchoredPositionX;
        rectTransform.anchoredPosition = value;
    }
    public static void SetAnchoredPositionY(this RectTransform rectTransform, float anchoredPositionY)
    {
        var value = rectTransform.anchoredPosition;
        value.y = anchoredPositionY;
        rectTransform.anchoredPosition = value;
    }
    public static void SetAnchoredPosition3DZ(this RectTransform rectTransform, float anchoredPositionZ)
    {
        var value = rectTransform.anchoredPosition3D;
        value.z = anchoredPositionZ;
        rectTransform.anchoredPosition3D = value;
    }
    public static void SetColorAlpha(this UnityEngine.UI.Graphic graphic, float alpha)
    {
        var value = graphic.color;
        value.a = alpha;
        graphic.color = value;
    }
    public static void SetFlexibleSize(this LayoutElement layoutElement, Vector2 flexibleSize)
    {
        layoutElement.flexibleWidth = flexibleSize.x;
        layoutElement.flexibleHeight = flexibleSize.y;
    }
    public static Vector2 GetFlexibleSize(this LayoutElement layoutElement)
    {
        return new Vector2(layoutElement.flexibleWidth, layoutElement.flexibleHeight);
    }
    public static void SetMinSize(this LayoutElement layoutElement, Vector2 size)
    {
        layoutElement.minWidth = size.x;
        layoutElement.minHeight = size.y;
    }
    public static Vector2 GetMinSize(this LayoutElement layoutElement)
    {
        return new Vector2(layoutElement.minWidth, layoutElement.minHeight);
    }
    public static void SetPreferredSize(this LayoutElement layoutElement, Vector2 size)
    {
        layoutElement.preferredWidth = size.x;
        layoutElement.preferredHeight = size.y;
    }
    public static Vector2 GetPreferredSize(this LayoutElement layoutElement)
    {
        return new Vector2(layoutElement.preferredWidth, layoutElement.preferredHeight);
    }
    #endregion
    public enum ToastStyle : uint
    {
        Blue = 0,
        Yellow = 1,
        Green = 2,
        Red = 3,
        White = 4
    }
}
