using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

public sealed class FirstPlayableModuleBoundaryTests
{
    private const string ModuleRootRelativePath = "Game/Scripts/FirstPlayable";
    private static readonly ModuleRule[] ModuleRules =
    {
        new ModuleRule("Domain", "GameDesinger.FirstPlayable.Domain", new[] { "UnityEngine", "MonoBehaviour", "GameDesinger.FirstPlayable.Data", "GameDesinger.FirstPlayable.Runtime", "GameDesinger.FirstPlayable.UI", "Totem" }),
        new ModuleRule("Data", "GameDesinger.FirstPlayable.Data", new[] { "UnityEngine.UI", "GameDesinger.FirstPlayable.Runtime", "GameDesinger.FirstPlayable.UI", "TotemUIService" }),
        new ModuleRule("Runtime", "GameDesinger.FirstPlayable.Runtime", Array.Empty<string>()),
        new ModuleRule("UI", "GameDesinger.FirstPlayable.UI", new[] { "GameDesinger.FirstPlayable.Data", "TotemDataService" }),
    };

    [Test]
    public void NewFirstPlayableModules_UseTheDeclaredFoldersAndNamespaces()
    {
        string assetsRoot = GetAssetsRoot();
        foreach (ModuleRule rule in ModuleRules)
        {
            string modulePath = Path.Combine(assetsRoot, ModuleRootRelativePath, rule.Name);
            Assert.That(Directory.Exists(modulePath), Is.True, $"Missing First Playable module directory: {modulePath}");

            string[] sourceFiles = Directory.GetFiles(modulePath, "*.cs", SearchOption.AllDirectories);
            Assert.That(sourceFiles.Length, Is.GreaterThan(0), $"Module needs an explicit source anchor: {rule.Name}");
            for (int i = 0; i < sourceFiles.Length; i++)
            {
                string source = File.ReadAllText(sourceFiles[i]);
                StringAssert.Contains($"namespace {rule.Namespace}", source, sourceFiles[i]);
            }
        }
    }

    [Test]
    public void NewFirstPlayableModules_DoNotCrossForbiddenBoundaries()
    {
        string assetsRoot = GetAssetsRoot();
        foreach (ModuleRule rule in ModuleRules)
        {
            string modulePath = Path.Combine(assetsRoot, ModuleRootRelativePath, rule.Name);
            string[] sourceFiles = Directory.GetFiles(modulePath, "*.cs", SearchOption.AllDirectories);
            for (int fileIndex = 0; fileIndex < sourceFiles.Length; fileIndex++)
            {
                string source = File.ReadAllText(sourceFiles[fileIndex]);
                for (int forbiddenIndex = 0; forbiddenIndex < rule.ForbiddenTokens.Length; forbiddenIndex++)
                {
                    string forbiddenToken = rule.ForbiddenTokens[forbiddenIndex];
                    Assert.That(source.IndexOf(forbiddenToken, StringComparison.Ordinal), Is.LessThan(0), $"{sourceFiles[fileIndex]} must not depend on {forbiddenToken}.");
                }
            }
        }
    }

    [Test]
    public void LegacyRuntimeServices_DoNotReferenceNewFirstPlayableNamespaces()
    {
        string legacyServicesPath = Path.Combine(GetAssetsRoot(), "Game/Scripts/Runtime/Services");
        string[] sourceFiles = Directory.GetFiles(legacyServicesPath, "*.cs", SearchOption.AllDirectories);
        for (int i = 0; i < sourceFiles.Length; i++)
        {
            string source = File.ReadAllText(sourceFiles[i]);
            Assert.That(source.IndexOf("GameDesinger.FirstPlayable.", StringComparison.Ordinal), Is.LessThan(0), $"Legacy service must not become a new module consumer: {sourceFiles[i]}");
        }
    }

    private static string GetAssetsRoot()
    {
        return UnityEngine.Application.dataPath;
    }

    private readonly struct ModuleRule
    {
        public ModuleRule(string name, string @namespace, string[] forbiddenTokens)
        {
            Name = name;
            Namespace = @namespace;
            ForbiddenTokens = forbiddenTokens ?? Array.Empty<string>();
        }

        public string Name { get; }

        public string Namespace { get; }

        public string[] ForbiddenTokens { get; }
    }
}
