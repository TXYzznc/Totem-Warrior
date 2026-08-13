using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class CameraGroupPanelTests
{
    private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags StaticMembers = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    private Type panelType;
    private object panel;

    [SetUp]
    public void SetUp()
    {
        Selection.activeObject = null;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        panelType = RequireType("CameraGroupPanel, Builtin.Editor");
        panel = Activator.CreateInstance(panelType);
        InvokeInstance("OnEnable");
    }

    [TearDown]
    public void TearDown()
    {
        Selection.activeObject = null;
        if (panel != null)
            InvokeInstance("OnDisable");
        panel = null;
        Undo.ClearAll();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    [Test]
    public void Discovery_IncludesInactiveSceneCamera_AndHierarchyGroupSupportsUndo()
    {
        GameObject cameraObject = new("InactiveCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.SetActive(false);

        IEnumerable records = (IEnumerable)InvokeInstance("RefreshCameraCache");
        Assert.That(records.Cast<object>().Any(record => GetRecordCamera(record) == camera), Is.True);
        Assert.That(GetGroupName(camera), Is.EqualTo(GetConstant("UngroupedName")));

        InvokeStatic("MoveToGroup", camera, "测试组");
        Assert.That(GetGroupName(camera), Is.EqualTo("测试组"));

        Undo.PerformUndo();
        Assert.That(GetGroupName(camera), Is.EqualTo(GetConstant("UngroupedName")));
    }

    [Test]
    public void TemporaryGameViewSwitch_RestoresEnabledAndTargetTextureStates()
    {
        Camera first = new GameObject("FirstCamera").AddComponent<Camera>();
        Camera second = new GameObject("SecondCamera").AddComponent<Camera>();
        RenderTexture targetTexture = new(32, 32, 0);
        first.enabled = true;
        first.targetTexture = targetTexture;
        second.enabled = false;

        InvokeInstance("BeginTemporaryGameViewSwitch", second);

        Assert.That(first.enabled, Is.False);
        Assert.That(first.targetTexture, Is.SameAs(targetTexture));
        Assert.That(second.enabled, Is.True);
        Assert.That(second.targetTexture, Is.Null);

        InvokeInstance("RestoreTemporaryGameViewSwitch");

        Assert.That(first.enabled, Is.True);
        Assert.That(first.targetTexture, Is.SameAs(targetTexture));
        Assert.That(second.enabled, Is.False);
        first.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(targetTexture);
    }

    [Test]
    public void OasisCityBuilder_CreatesFourteenGroupedReviewCameras_WithBuildingCloseupsAndOneDefaultActive()
    {
        GameObject reviewRoot = new("99_Review");
        Type builderType = RequireType("Game.EditorTools.OasisCity.OasisCityMapBuilder, Game.OasisCityMapBuilder.Editor");
        MethodInfo buildMethod = builderType.GetMethod("BuildReviewObjects", StaticMembers);
        Assert.That(buildMethod, Is.Not.Null);
        buildMethod.Invoke(null, new object[] { reviewRoot.transform });

        Transform groupsRoot = reviewRoot.transform.Find(GetConstant("CameraGroupsRootName"));
        Assert.That(groupsRoot, Is.Not.Null);
        Assert.That(groupsRoot.childCount, Is.EqualTo(4));
        Transform closeupGroup = groupsRoot.Find("建筑特写");
        Assert.That(closeupGroup, Is.Not.Null);
        Assert.That(closeupGroup.childCount, Is.EqualTo(6));

        Camera[] cameras = groupsRoot.GetComponentsInChildren<Camera>(true);
        Assert.That(cameras.Length, Is.EqualTo(14));
        Assert.That(cameras.Any(camera => camera.name == "CAM_Building_Tower_BF01"), Is.True);
        Assert.That(cameras.Any(camera => camera.name == "CAM_Building_Bazaar_BF24"), Is.True);
        Assert.That(cameras.Count(camera => camera.enabled), Is.EqualTo(1));
        Assert.That(cameras.Single(camera => camera.enabled).CompareTag("MainCamera"), Is.True);
    }

    private object InvokeInstance(string methodName, params object[] arguments)
    {
        MethodInfo method = panelType.GetMethod(methodName, InstanceMembers);
        Assert.That(method, Is.Not.Null, $"Missing instance method: {methodName}");
        return method.Invoke(panel, arguments);
    }

    private object InvokeStatic(string methodName, params object[] arguments)
    {
        MethodInfo method = panelType.GetMethod(methodName, StaticMembers);
        Assert.That(method, Is.Not.Null, $"Missing static method: {methodName}");
        return method.Invoke(null, arguments);
    }

    private string GetGroupName(Camera camera) => (string)InvokeStatic("GetGroupName", camera);

    private string GetConstant(string fieldName)
    {
        FieldInfo field = panelType.GetField(fieldName, StaticMembers);
        Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
        return (string)field.GetValue(null);
    }

    private static Camera GetRecordCamera(object record)
    {
        FieldInfo field = record.GetType().GetField("Camera", InstanceMembers);
        return (Camera)field.GetValue(record);
    }

    private static Type RequireType(string assemblyQualifiedName)
    {
        Type type = Type.GetType(assemblyQualifiedName, false);
        Assert.That(type, Is.Not.Null, $"Type not found: {assemblyQualifiedName}");
        return type;
    }
}
