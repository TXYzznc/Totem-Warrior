#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Produces a reviewable TattooMap batch for all ActorCommonM02 right-facing frames.
/// The source frames use top-left image coordinates, while Texture2D uses bottom-left pixel coordinates.
/// A map pixel stores local part UV in R/G, part id in B, and the approved skin mask in A.
/// </summary>
internal static class ActorCommonM02TattooMapTool
{
    private const int FramePixels = 512;
    private const string SpriteDirectory = "Assets/Game/Sprite/Actors/ActorCommonM02";
    private const string MapDirectory = SpriteDirectory + "/TattooMaps/RollRight";
    private const string RightMapDirectory = SpriteDirectory + "/TattooMaps/Right";
    private const string LegendDirectory = SpriteDirectory + "/TattooMaps/Legend";
    private const string LegendSourcePath = SpriteDirectory + "/actor_common_m02_idle_down_01.png";
    private const string LegendMapPath = LegendDirectory + "/actor_common_m02_idle_down_01_tattoo_map_legend.png";
    private const string LegendOverlayPath = LegendDirectory + "/actor_common_m02_idle_down_01_six_region_overlay.png";
    private const string MapSetDirectory = "Assets/Game/Config/TattooVisual";
    private const string MapSetPath = MapSetDirectory + "/ActorCommonM02RollRightTattooMapSet.asset";
    private const string AuthoringAssetPath = MapSetDirectory + "/ActorCommonM02TattooRegionAuthoring.asset";
    private const string PatternDirectory = "Assets/Game/Sprite/Tattoo";
    private const string PatternAtlasPath = PatternDirectory + "/TattooPatternAtlas_Prototype.png";
    private const string MaterialDirectory = "Assets/Game/Material/Characters";
    private const string MaterialPath = MaterialDirectory + "/ActorCommonM02TattooPrototype.mat";
    private const string PreviewMaterialDirectory = MaterialDirectory + "/TattooPreview";
    private const string TattooShaderPath = "Assets/Game/Shader/TotemTattooSprite.shader";
    private const string PlayerPrefabPath = "Assets/Game/Prefabs/Entity/Actors/Player.prefab";
    private const string ShaderPreviewScenePath = "Assets/Game/Scene/ActorCommonM02RollRightTattooPreview.unity";
    private const string RightDirectionPreviewScenePath = "Assets/Game/Scene/ActorCommonM02RightTattooMapReview.unity";
    private const string RegionLegendScenePath = "Assets/Game/Scene/ActorCommonM02TattooRegionLegend.unity";
    private const string ReviewRelativeDirectory = "openspec/changes/dynamic-tattoo-visuals/art/review/roll_right";
    private const string Action = "roll";
    private const string Direction = "right";
    private const int FrameCount = 8;

    private static readonly Color32[] PartDebugColors =
    {
        new Color32(244, 87, 89, 255),
        new Color32(241, 176, 67, 255),
        new Color32(90, 197, 119, 255),
        new Color32(75, 157, 230, 255),
        new Color32(174, 95, 219, 255),
        new Color32(74, 212, 209, 255),
    };

    // Each region is an anatomical anchor, not a screen-space rectangle. The four vertices carry
    // stable local UV corners: upper/outer=(0,0), upper/inner=(1,0), lower/inner=(1,1),
    // lower/outer=(0,1). In each subsequent frame the same Zone moves with the corresponding
    // body surface. A hidden zone is omitted rather than projected onto a different visible part.
    // IsSkinPixel provides a second, pixel-level guard against clothing and transparent pixels.
    private static readonly TattooRegion[][] RollRightRegions =
    {
        new[]
        {
            Region(2, TattooZone.TorsoChest, 288,343, 321,347, 317,371, 288,369),
            Region(3, TattooZone.LeftArmOuter, 218,346, 246,357, 241,383, 216,374),
            Region(4, TattooZone.RightArmOuter, 357,391, 377,396, 375,425, 356,418),
            Region(5, TattooZone.LeftLegOuterThigh, 176,426, 204,428, 200,450, 179,449),
            Region(6, TattooZone.RightLegOuterThigh, 287,390, 316,397, 312,422, 286,414),
        },
        new[]
        {
            Region(2, TattooZone.TorsoChest, 283,364, 314,367, 311,388, 284,385),
            Region(3, TattooZone.LeftArmOuter, 219,364, 247,373, 242,398, 218,390),
            Region(4, TattooZone.RightArmOuter, 357,399, 379,404, 377,428, 356,423),
            Region(5, TattooZone.LeftLegOuterThigh, 185,432, 212,435, 208,457, 185,454),
            Region(6, TattooZone.RightLegOuterThigh, 270,402, 297,407, 293,430, 270,425),
        },
        new[]
        {
            Region(3, TattooZone.LeftArmOuter, 234,394, 261,401, 256,424, 232,418),
            Region(4, TattooZone.RightArmOuter, 347,429, 371,432, 367,453, 344,450),
            Region(5, TattooZone.LeftLegOuterThigh, 181,445, 208,448, 205,469, 180,466),
            Region(6, TattooZone.RightLegOuterThigh, 258,441, 286,445, 282,467, 257,463),
        },
        new[]
        {
            Region(3, TattooZone.LeftArmOuter, 244,403, 269,407, 264,429, 241,425),
            Region(4, TattooZone.RightArmOuter, 332,401, 354,405, 351,423, 332,420),
            Region(5, TattooZone.LeftLegOuterThigh, 218,452, 244,457, 239,477, 216,473),
            Region(6, TattooZone.RightLegOuterThigh, 286,453, 311,457, 309,478, 285,475),
        },
        new[]
        {
            Region(3, TattooZone.LeftArmOuter, 339,427, 364,433, 360,451, 339,447),
            Region(5, TattooZone.LeftLegOuterThigh, 216,299, 241,303, 246,325, 220,327),
            Region(6, TattooZone.RightLegOuterThigh, 270,323, 296,326, 291,350, 268,347),
        },
        new[]
        {
            Region(3, TattooZone.LeftArmOuter, 307,382, 336,388, 330,407, 305,401),
            Region(4, TattooZone.RightArmOuter, 369,404, 394,409, 389,430, 368,425),
            Region(5, TattooZone.LeftLegOuterThigh, 250,422, 277,425, 272,448, 249,445),
        },
        new[]
        {
            Region(2, TattooZone.TorsoChest, 284,349, 316,353, 312,377, 284,373),
            Region(3, TattooZone.LeftArmOuter, 214,349, 243,360, 238,384, 213,376),
            Region(4, TattooZone.RightArmOuter, 356,396, 379,401, 377,424, 355,420),
            Region(5, TattooZone.LeftLegOuterThigh, 174,438, 202,441, 198,464, 173,461),
            Region(6, TattooZone.RightLegOuterThigh, 282,407, 310,412, 306,436, 281,431),
        },
        new[]
        {
            Region(2, TattooZone.TorsoChest, 284,353, 315,356, 311,381, 284,377),
            Region(3, TattooZone.LeftArmOuter, 202,345, 230,356, 225,379, 201,371),
            Region(4, TattooZone.RightArmOuter, 360,374, 388,380, 383,402, 359,397),
            Region(5, TattooZone.LeftLegOuterThigh, 166,440, 196,444, 191,470, 165,466),
            Region(6, TattooZone.RightLegOuterThigh, 288,443, 318,449, 313,475, 287,470),
        },
    };

    // This is a readable front-facing reference pose, not an additional production animation map.
    // It exposes the six semantic body regions that the roll sample cannot show while the body is
    // curled or occluded. The same skin-pixel gate used by production maps still excludes clothes.
    private static readonly TattooRegion[] IdleDownLegendRegions =
    {
        Region(1, TattooZone.HeadTemple, 239,42, 259,42, 258,63, 239,63),
        Region(2, TattooZone.TorsoChest, 221,120, 289,120, 285,195, 221,195),
        Region(3, TattooZone.LeftArmOuter, 164,119, 195,119, 192,230, 163,230),
        Region(4, TattooZone.RightArmOuter, 316,119, 347,119, 349,230, 319,230),
        Region(5, TattooZone.LeftLegOuterThigh, 193,305, 226,305, 226,404, 194,404),
        Region(6, TattooZone.RightLegOuterThigh, 280,305, 313,305, 312,404, 280,404),
    };

    // Every entry below was placed against its individual right-facing source frame. A region is
    // omitted when that body surface is not reliably visible; it is never borrowed from clothing
    // or an adjacent limb. Box is only a compact notation for the four manually chosen corners.
    private static readonly ManualFrame[] ManualRightFrames =
    {
        Frame("idle", 1, Box(1,TattooZone.HeadTemple,267,50,286,76), Box(2,TattooZone.TorsoChest,244,172,269,212), Box(4,TattooZone.RightArmOuter,205,105,235,217), Box(6,TattooZone.RightLegOuterThigh,237,300,264,402)),
        Frame("idle", 2, Box(1,TattooZone.HeadTemple,267,50,286,76), Box(2,TattooZone.TorsoChest,244,172,269,212), Box(4,TattooZone.RightArmOuter,205,105,235,217), Box(6,TattooZone.RightLegOuterThigh,237,300,264,402)),
        Frame("idle", 3, Box(1,TattooZone.HeadTemple,267,50,286,76), Box(2,TattooZone.TorsoChest,244,172,269,212), Box(4,TattooZone.RightArmOuter,205,105,235,217), Box(6,TattooZone.RightLegOuterThigh,237,300,264,402)),
        Frame("idle", 4, Box(1,TattooZone.HeadTemple,267,50,286,76), Box(2,TattooZone.TorsoChest,244,172,269,212), Box(4,TattooZone.RightArmOuter,205,105,235,217), Box(6,TattooZone.RightLegOuterThigh,237,300,264,402)),

        Frame("walk", 1, Box(1,TattooZone.HeadTemple,252,27,273,55), Box(2,TattooZone.TorsoChest,240,111,270,188), Box(3,TattooZone.LeftArmOuter,177,109,204,226), Box(4,TattooZone.RightArmOuter,307,139,333,221), Box(5,TattooZone.LeftLegOuterThigh,171,300,211,393), Box(6,TattooZone.RightLegOuterThigh,278,292,317,398)),
        Frame("walk", 2, Box(1,TattooZone.HeadTemple,251,17,272,46), Box(2,TattooZone.TorsoChest,238,101,268,180), Box(3,TattooZone.LeftArmOuter,181,101,208,225), Box(4,TattooZone.RightArmOuter,304,138,332,215), Box(5,TattooZone.LeftLegOuterThigh,205,298,240,391), Box(6,TattooZone.RightLegOuterThigh,263,288,295,380)),
        Frame("walk", 3, Box(1,TattooZone.HeadTemple,251,12,272,40), Box(2,TattooZone.TorsoChest,239,96,267,173), Box(3,TattooZone.LeftArmOuter,205,93,230,213), Box(5,TattooZone.LeftLegOuterThigh,152,304,204,388), Box(6,TattooZone.RightLegOuterThigh,273,287,311,397)),
        Frame("walk", 4, Box(1,TattooZone.HeadTemple,252,15,273,45), Box(2,TattooZone.TorsoChest,239,100,269,180), Box(3,TattooZone.LeftArmOuter,165,102,192,222), Box(4,TattooZone.RightArmOuter,296,143,322,218), Box(5,TattooZone.LeftLegOuterThigh,236,288,271,363), Box(6,TattooZone.RightLegOuterThigh,270,303,302,389)),
        Frame("walk", 5, Box(1,TattooZone.HeadTemple,253,22,274,50), Box(2,TattooZone.TorsoChest,239,106,269,183), Box(3,TattooZone.LeftArmOuter,207,102,233,217), Box(5,TattooZone.LeftLegOuterThigh,222,302,255,388), Box(6,TattooZone.RightLegOuterThigh,269,302,302,392)),
        Frame("walk", 6, Box(1,TattooZone.HeadTemple,253,21,274,50), Box(2,TattooZone.TorsoChest,240,104,270,183), Box(3,TattooZone.LeftArmOuter,175,103,202,222), Box(4,TattooZone.RightArmOuter,304,142,331,218), Box(5,TattooZone.LeftLegOuterThigh,154,306,204,391), Box(6,TattooZone.RightLegOuterThigh,273,295,307,392)),

        Frame("sprint", 1, Box(1,TattooZone.HeadTemple,338,184,359,207), Box(2,TattooZone.TorsoChest,285,226,348,289), Box(3,TattooZone.LeftArmOuter,187,212,244,271), Box(4,TattooZone.RightArmOuter,366,258,402,308), Box(5,TattooZone.LeftLegOuterThigh,111,354,210,420), Box(6,TattooZone.RightLegOuterThigh,260,330,333,403)),
        Frame("sprint", 2, Box(1,TattooZone.HeadTemple,345,178,366,202), Box(2,TattooZone.TorsoChest,288,219,350,282), Box(3,TattooZone.LeftArmOuter,209,200,269,254), Box(4,TattooZone.RightArmOuter,369,252,403,300), Box(5,TattooZone.LeftLegOuterThigh,96,349,201,414), Box(6,TattooZone.RightLegOuterThigh,267,307,344,367)),
        Frame("sprint", 3, Box(1,TattooZone.HeadTemple,339,183,360,207), Box(2,TattooZone.TorsoChest,286,226,348,286), Box(3,TattooZone.LeftArmOuter,187,210,245,268), Box(4,TattooZone.RightArmOuter,370,258,404,304), Box(5,TattooZone.LeftLegOuterThigh,100,359,208,423), Box(6,TattooZone.RightLegOuterThigh,261,332,332,400)),
        Frame("sprint", 4, Box(1,TattooZone.HeadTemple,340,180,361,205), Box(2,TattooZone.TorsoChest,286,221,348,284), Box(3,TattooZone.LeftArmOuter,203,204,260,262), Box(4,TattooZone.RightArmOuter,369,249,404,299), Box(5,TattooZone.LeftLegOuterThigh,104,351,206,414), Box(6,TattooZone.RightLegOuterThigh,283,313,359,368)),
        Frame("sprint", 5, Box(1,TattooZone.HeadTemple,342,180,363,204), Box(2,TattooZone.TorsoChest,288,220,351,282), Box(3,TattooZone.LeftArmOuter,195,203,253,260), Box(4,TattooZone.RightArmOuter,372,248,407,298), Box(5,TattooZone.LeftLegOuterThigh,95,349,201,413), Box(6,TattooZone.RightLegOuterThigh,285,322,364,380)),
        Frame("sprint", 6, Box(1,TattooZone.HeadTemple,339,184,360,208), Box(2,TattooZone.TorsoChest,285,226,348,289), Box(3,TattooZone.LeftArmOuter,185,212,243,272), Box(4,TattooZone.RightArmOuter,367,258,402,307), Box(5,TattooZone.LeftLegOuterThigh,111,356,209,421), Box(6,TattooZone.RightLegOuterThigh,260,331,332,403)),

        Frame("hit", 1, Box(1,TattooZone.HeadTemple,253,41,274,67), Box(2,TattooZone.TorsoChest,239,117,274,196), Box(3,TattooZone.LeftArmOuter,197,102,225,223), Box(5,TattooZone.LeftLegOuterThigh,199,294,231,397), Box(6,TattooZone.RightLegOuterThigh,279,294,312,399)),
        Frame("hit", 2, Box(1,TattooZone.HeadTemple,173,55,195,82), Box(2,TattooZone.TorsoChest,183,139,224,217), Box(3,TattooZone.LeftArmOuter,130,118,161,239), Box(4,TattooZone.RightArmOuter,290,176,324,225), Box(5,TattooZone.LeftLegOuterThigh,213,299,246,397), Box(6,TattooZone.RightLegOuterThigh,301,310,339,400)),
        Frame("hit", 3, Box(1,TattooZone.HeadTemple,280,177,301,201), Box(3,TattooZone.LeftArmOuter,202,214,240,281), Box(4,TattooZone.RightArmOuter,316,256,352,300), Box(5,TattooZone.LeftLegOuterThigh,215,365,246,395), Box(6,TattooZone.RightLegOuterThigh,275,316,315,384)),
        Frame("hit", 4, Box(1,TattooZone.HeadTemple,253,41,274,67), Box(2,TattooZone.TorsoChest,239,117,274,196), Box(3,TattooZone.LeftArmOuter,197,102,225,223), Box(5,TattooZone.LeftLegOuterThigh,199,294,231,397), Box(6,TattooZone.RightLegOuterThigh,279,294,312,399)),

        Frame("attack", 1, Box(1,TattooZone.HeadTemple,246,40,267,67), Box(2,TattooZone.TorsoChest,222,116,274,198), Box(3,TattooZone.LeftArmOuter,166,101,198,219), Box(4,TattooZone.RightArmOuter,299,123,327,219), Box(5,TattooZone.LeftLegOuterThigh,191,292,224,398), Box(6,TattooZone.RightLegOuterThigh,280,292,313,400)),
        Frame("attack", 2, Box(1,TattooZone.HeadTemple,189,57,211,82), Box(2,TattooZone.TorsoChest,189,135,236,211), Box(3,TattooZone.LeftArmOuter,130,119,162,238), Box(4,TattooZone.RightArmOuter,283,178,321,226), Box(5,TattooZone.LeftLegOuterThigh,217,292,248,394), Box(6,TattooZone.RightLegOuterThigh,304,304,341,399)),
        Frame("attack", 3, Box(1,TattooZone.HeadTemple,282,157,304,183), Box(3,TattooZone.LeftArmOuter,202,202,242,277), Box(4,TattooZone.RightArmOuter,309,251,345,298), Box(5,TattooZone.LeftLegOuterThigh,203,318,250,388), Box(6,TattooZone.RightLegOuterThigh,274,307,317,376)),
        Frame("attack", 4, Box(1,TattooZone.HeadTemple,246,40,267,67), Box(2,TattooZone.TorsoChest,222,116,274,198), Box(3,TattooZone.LeftArmOuter,166,101,198,219), Box(4,TattooZone.RightArmOuter,299,123,327,219), Box(5,TattooZone.LeftLegOuterThigh,191,292,224,398), Box(6,TattooZone.RightLegOuterThigh,280,292,313,400)),
        Frame("attack", 5, Box(1,TattooZone.HeadTemple,246,40,267,67), Box(2,TattooZone.TorsoChest,222,116,274,198), Box(3,TattooZone.LeftArmOuter,166,101,198,219), Box(4,TattooZone.RightArmOuter,299,123,327,219), Box(5,TattooZone.LeftLegOuterThigh,191,292,224,398), Box(6,TattooZone.RightLegOuterThigh,280,292,313,400)),
        Frame("attack", 6, Box(1,TattooZone.HeadTemple,246,40,267,67), Box(2,TattooZone.TorsoChest,222,116,274,198), Box(3,TattooZone.LeftArmOuter,166,101,198,219), Box(4,TattooZone.RightArmOuter,299,123,327,219), Box(5,TattooZone.LeftLegOuterThigh,191,292,224,398), Box(6,TattooZone.RightLegOuterThigh,280,292,313,400)),

        Frame("death", 1, Box(1,TattooZone.HeadTemple,220,65,241,92), Box(2,TattooZone.TorsoChest,193,146,242,218), Box(3,TattooZone.LeftArmOuter,151,130,180,245), Box(4,TattooZone.RightArmOuter,285,184,321,231), Box(5,TattooZone.LeftLegOuterThigh,222,292,252,389), Box(6,TattooZone.RightLegOuterThigh,304,302,341,395)),
        Frame("death", 2, Box(1,TattooZone.HeadTemple,174,99,196,126), Box(2,TattooZone.TorsoChest,172,175,219,245), Box(3,TattooZone.LeftArmOuter,120,155,151,269), Box(4,TattooZone.RightArmOuter,285,202,321,250), Box(5,TattooZone.LeftLegOuterThigh,222,300,252,396), Box(6,TattooZone.RightLegOuterThigh,310,308,347,397)),
        Frame("death", 3, Box(1,TattooZone.HeadTemple,282,179,304,205), Box(3,TattooZone.LeftArmOuter,201,220,240,287), Box(4,TattooZone.RightArmOuter,309,263,346,309), Box(5,TattooZone.LeftLegOuterThigh,201,332,247,394), Box(6,TattooZone.RightLegOuterThigh,273,319,315,383)),
        Frame("death", 4, Box(1,TattooZone.HeadTemple,294,283,315,306), Box(3,TattooZone.LeftArmOuter,221,303,260,360), Box(4,TattooZone.RightArmOuter,322,331,358,370), Box(5,TattooZone.LeftLegOuterThigh,145,361,204,408), Box(6,TattooZone.RightLegOuterThigh,252,340,294,385)),
        // The collapsing and prone poses fold the near-side arm across the face. Deliberately
        // leave that ambiguous surface unmapped instead of letting a right-arm tattoo leak onto
        // the head; hidden / unreliable skin is allowed to disappear for the frame.
        Frame("death", 5, Box(3,TattooZone.LeftArmOuter,217,349,253,402), Box(5,TattooZone.LeftLegOuterThigh,127,389,193,430), Box(6,TattooZone.RightLegOuterThigh,241,373,287,413)),
        Frame("death", 6, Box(3,TattooZone.LeftArmOuter,231,405,273,446), Box(5,TattooZone.LeftLegOuterThigh,119,423,185,455), Box(6,TattooZone.RightLegOuterThigh,236,410,282,443)),
        Frame("death", 7, Box(3,TattooZone.LeftArmOuter,248,432,291,464), Box(5,TattooZone.LeftLegOuterThigh,134,445,195,473), Box(6,TattooZone.RightLegOuterThigh,235,434,282,461)),
        Frame("death", 8, Box(3,TattooZone.LeftArmOuter,245,439,287,470), Box(5,TattooZone.LeftLegOuterThigh,132,451,193,478), Box(6,TattooZone.RightLegOuterThigh,231,440,278,466)),
    };


    [MenuItem("Game/Totem/Tattoo/Generate Right Direction TattooMaps")]
    private static void GenerateRollRightSample()
    {
        GenerateRightDirectionTattooMaps();
    }

    internal static void GenerateRightDirectionTattooMaps()
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrWhiteSpace(projectRoot))
        {
            throw new InvalidOperationException("Could not resolve the Unity project root.");
        }

        EnsureFolder("Assets/Game/Config");
        EnsureFolder(MapSetDirectory);
        EnsureFolder("Assets/Game/Sprite/Actors/ActorCommonM02/TattooMaps");
        EnsureFolder(MapDirectory);
        EnsureFolder(RightMapDirectory);
        for (int index = 0; index < ManualRightFrames.Length; index++)
        {
            EnsureFolder(GetRightActionDirectory(ManualRightFrames[index].Action));
        }
        EnsureFolder(LegendDirectory);
        EnsureFolder(PatternDirectory);
        EnsureFolder("Assets/Game/Material");
        EnsureFolder(MaterialDirectory);
        EnsureFolder(PreviewMaterialDirectory);
        string reviewDirectory = Path.Combine(projectRoot, ReviewRelativeDirectory);
        Directory.CreateDirectory(reviewDirectory);

        var sourcePaths = new string[FrameCount];
        var changedImporterPaths = new List<string>(FrameCount);
        for (int index = 0; index < FrameCount; index++)
        {
            sourcePaths[index] = SpriteDirectory + "/" + GetFrameName(index + 1);
            var importer = AssetImporter.GetAtPath(sourcePaths[index]) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("M02 roll-right source texture is missing: " + sourcePaths[index]);
            }

            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
                changedImporterPaths.Add(sourcePaths[index]);
            }
        }

        EnsureReadableSource(LegendSourcePath, changedImporterPaths);
        for (int index = 0; index < ManualRightFrames.Length; index++)
        {
            EnsureReadableSource(GetSourcePath(ManualRightFrames[index].Action, Direction, ManualRightFrames[index].Frame), changedImporterPaths);
        }

        try
        {
            GenerateMaps(sourcePaths, reviewDirectory);
            GenerateManualRightMaps(projectRoot);
            GenerateStandardRegionLegend(projectRoot);
        }
        finally
        {
            for (int index = 0; index < changedImporterPaths.Count; index++)
            {
                var importer = AssetImporter.GetAtPath(changedImporterPaths[index]) as TextureImporter;
                if (importer != null)
                {
                    importer.isReadable = false;
                    importer.SaveAndReimport();
                }
            }
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureMapImporters();
        TotemTattooFrameMapSet mapSet = CreateOrUpdateMapSet();
        Texture2D patternAtlas = CreateOrUpdatePrototypePatternAtlas();
        Material material = CreateOrUpdateTattooMaterial(patternAtlas);
        BindPlayerPresenter(mapSet, material, patternAtlas);
        CreateShaderPreviewScene(mapSet, material, patternAtlas);
        CreateRightDirectionPreviewScene(mapSet, material, patternAtlas);
        CreateRegionLegendScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ValidateRightDirectionTattooMaps();
        Debug.Log("Generated ActorCommonM02 right-facing TattooMap batch: 42 manually reviewed frame bindings, prototype atlas/material and Player presenter wiring.");
    }

    /// <summary>
    /// Generates only the direction selected in the marker window. Right keeps its reviewed
    /// fallback batch; the other directions export exclusively the artist-authored regions so
    /// unmarked frames can never receive a guessed anatomical mapping.
    /// </summary>
    internal static void GenerateCurrentDirectionTattooMaps(string direction)
    {
        if (string.IsNullOrWhiteSpace(direction))
        {
            throw new ArgumentException("A TattooMap direction is required.", nameof(direction));
        }

        direction = direction.Trim().ToLowerInvariant();
        if (direction == Direction)
        {
            GenerateRightDirectionTattooMaps();
            return;
        }

        if (direction != "down" && direction != "up" && direction != "left")
        {
            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unsupported TattooMap direction.");
        }

        TattooMapRegionAuthoringAsset authoring = AssetDatabase.LoadAssetAtPath<TattooMapRegionAuthoringAsset>(AuthoringAssetPath);
        if (authoring == null)
        {
            throw new InvalidOperationException("No TattooMap authoring asset exists. Mark at least one frame before generating.");
        }

        var authoredFrames = new List<TattooMapFrameAuthoring>();
        for (int index = 0; index < authoring.Frames.Count; index++)
        {
            TattooMapFrameAuthoring frame = authoring.Frames[index];
            if (frame != null && frame.direction == direction && frame.regions != null && frame.regions.Count > 0)
            {
                authoredFrames.Add(frame);
            }
        }

        if (authoredFrames.Count == 0)
        {
            throw new InvalidOperationException("当前方向没有已手工标记的帧：" + direction);
        }

        EnsureFolder("Assets/Game/Sprite/Actors/ActorCommonM02/TattooMaps");
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrWhiteSpace(projectRoot))
        {
            throw new InvalidOperationException("Could not resolve the Unity project root.");
        }

        var changedImporterPaths = new List<string>(authoredFrames.Count);
        var mapPaths = new List<string>(authoredFrames.Count);
        try
        {
            for (int index = 0; index < authoredFrames.Count; index++)
            {
                TattooMapFrameAuthoring frame = authoredFrames[index];
                string sourcePath = GetSourcePath(frame.action, direction, frame.frame);
                EnsureReadableSource(sourcePath, changedImporterPaths);

                Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
                if (source == null || source.width != FramePixels || source.height != FramePixels)
                {
                    throw new InvalidOperationException("Expected a readable 512x512 source frame: " + sourcePath);
                }

                Color32[] sourcePixels = source.GetPixels32();
                var mapPixels = new Color32[sourcePixels.Length];
                var debugPixels = new Color32[sourcePixels.Length];
                WriteFrameMap(sourcePixels, mapPixels, debugPixels, GetEffectiveRegions(frame.action, direction, frame.frame, Array.Empty<TattooRegion>()), frame.skinTolerance);

                EnsureFolder(GetDirectionActionDirectory(direction, frame.action));
                string mapPath = GetDirectionMapPath(frame.action, direction, frame.frame);
                WritePng(mapPath, mapPixels);
                mapPaths.Add(mapPath);

                string reviewDirectory = Path.Combine(projectRoot, ReviewRelativeDirectory, "authored", direction, frame.action);
                Directory.CreateDirectory(reviewDirectory);
                string baseName = GetFrameName(frame.action, direction, frame.frame).Replace(".png", string.Empty);
                WritePng(Path.Combine(reviewDirectory, baseName + "_tattoo_map_review.png"), debugPixels);
                WritePng(Path.Combine(reviewDirectory, baseName + "_tattoo_composite_preview.png"), BuildCompositePreview(sourcePixels, mapPixels));
            }
        }
        finally
        {
            for (int index = 0; index < changedImporterPaths.Count; index++)
            {
                var importer = AssetImporter.GetAtPath(changedImporterPaths[index]) as TextureImporter;
                if (importer != null)
                {
                    importer.isReadable = false;
                    importer.SaveAndReimport();
                }
            }
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        for (int index = 0; index < mapPaths.Count; index++)
        {
            ConfigureTattooMapImporter(mapPaths[index]);
        }

        TotemTattooFrameMapSet mapSet = CreateOrUpdateMapSet();
        AssetDatabase.SaveAssets();
        ValidateAuthoredDirectionTattooMaps(mapSet, direction, authoredFrames);
        Debug.Log("Generated ActorCommonM02 authored TattooMaps for direction " + direction + ": " + authoredFrames.Count + " frame(s).");
    }

    [MenuItem("Game/Totem/Tattoo/Validate Right Direction TattooMaps")]
    private static void ValidateRollRightSample()
    {
        ValidateRightDirectionTattooMaps();
    }

    internal static void ValidateRightDirectionTattooMaps()
    {
        TotemTattooFrameMapSet mapSet = AssetDatabase.LoadAssetAtPath<TotemTattooFrameMapSet>(MapSetPath);
        if (mapSet == null || mapSet.Count != FrameCount + ManualRightFrames.Length)
        {
            throw new InvalidOperationException("Right-facing TattooMap set must contain every reviewed binding.");
        }

        for (int index = 0; index < ManualRightFrames.Length; index++)
        {
            ManualFrame frame = ManualRightFrames[index];
            ValidateFrameBinding(mapSet, GetSourcePath(frame.Action, Direction, frame.Frame), GetRightMapPath(frame.Action, frame.Frame));
        }

        for (int index = 0; index < FrameCount; index++)
        {
            string sourcePath = SpriteDirectory + "/" + GetFrameName(index + 1);
            string mapPath = MapDirectory + "/" + GetMapFileName(index + 1);
            Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
            Texture2D map = AssetDatabase.LoadAssetAtPath<Texture2D>(mapPath);
            var importer = AssetImporter.GetAtPath(mapPath) as TextureImporter;
            if (source == null || map == null || source.width != FramePixels || source.height != FramePixels || map.width != FramePixels || map.height != FramePixels)
            {
                throw new InvalidOperationException("TattooMap size mismatch: " + mapPath);
            }

            if (importer == null || importer.sRGBTexture || importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed || importer.filterMode != FilterMode.Point)
            {
                throw new InvalidOperationException("TattooMap importer must be linear, point-filtered, non-mipmapped and uncompressed: " + mapPath);
            }

            Sprite sourceSprite = AssetDatabase.LoadAssetAtPath<Sprite>(sourcePath);
            if (!mapSet.TryGetTattooMap(sourceSprite, out Texture2D boundMap) || boundMap != map)
            {
                throw new InvalidOperationException("TattooMap binding is missing or incorrect for " + sourcePath);
            }
        }

        ValidatePlayerPresenter(mapSet);
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ShaderPreviewScenePath) == null)
        {
            throw new InvalidOperationException("Tattoo shader preview scene is missing.");
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RightDirectionPreviewScenePath) == null)
        {
            throw new InvalidOperationException("Right-direction TattooMap review scene is missing.");
        }

        ValidateStandardRegionLegend();

        Debug.Log("ActorCommonM02 RollRight TattooMap sample validation passed.");
    }

    internal static void ValidateCurrentDirectionTattooMaps(string direction)
    {
        if (string.Equals(direction, Direction, StringComparison.OrdinalIgnoreCase))
        {
            ValidateRightDirectionTattooMaps();
            return;
        }

        TattooMapRegionAuthoringAsset authoring = AssetDatabase.LoadAssetAtPath<TattooMapRegionAuthoringAsset>(AuthoringAssetPath);
        var authoredFrames = new List<TattooMapFrameAuthoring>();
        if (authoring != null)
        {
            for (int index = 0; index < authoring.Frames.Count; index++)
            {
                TattooMapFrameAuthoring frame = authoring.Frames[index];
                if (frame != null && frame.direction == direction && frame.regions != null && frame.regions.Count > 0)
                {
                    authoredFrames.Add(frame);
                }
            }
        }

        if (authoredFrames.Count == 0)
        {
            throw new InvalidOperationException("当前方向没有可验证的已手工标记帧：" + direction);
        }

        TotemTattooFrameMapSet mapSet = AssetDatabase.LoadAssetAtPath<TotemTattooFrameMapSet>(MapSetPath);
        ValidateAuthoredDirectionTattooMaps(mapSet, direction, authoredFrames);
    }

    [MenuItem("Game/Totem/Tattoo/Open Right Direction TattooMap Review")]
    private static void OpenRightDirectionPreviewScene()
    {
        if (!File.Exists(RightDirectionPreviewScenePath))
        {
            throw new InvalidOperationException("Generate the right-direction TattooMaps before opening the review scene.");
        }

        EditorSceneManager.OpenScene(RightDirectionPreviewScenePath, OpenSceneMode.Single);
    }

    private static void ValidateFrameBinding(TotemTattooFrameMapSet mapSet, string sourcePath, string mapPath)
    {
        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
        Texture2D map = AssetDatabase.LoadAssetAtPath<Texture2D>(mapPath);
        var importer = AssetImporter.GetAtPath(mapPath) as TextureImporter;
        if (source == null || map == null || source.width != FramePixels || source.height != FramePixels || map.width != FramePixels || map.height != FramePixels)
        {
            throw new InvalidOperationException("TattooMap size mismatch: " + mapPath);
        }

        if (importer == null || importer.sRGBTexture || importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed || importer.filterMode != FilterMode.Point)
        {
            throw new InvalidOperationException("TattooMap importer must be linear, point-filtered, non-mipmapped and uncompressed: " + mapPath);
        }

        Sprite sourceSprite = AssetDatabase.LoadAssetAtPath<Sprite>(sourcePath);
        if (!mapSet.TryGetTattooMap(sourceSprite, out Texture2D boundMap) || boundMap != map)
        {
            throw new InvalidOperationException("TattooMap binding is missing or incorrect for " + sourcePath);
        }
    }

    private static void ValidateAuthoredDirectionTattooMaps(TotemTattooFrameMapSet mapSet, string direction, List<TattooMapFrameAuthoring> authoredFrames)
    {
        if (mapSet == null)
        {
            throw new InvalidOperationException("TattooMap set is missing.");
        }

        for (int index = 0; index < authoredFrames.Count; index++)
        {
            TattooMapFrameAuthoring frame = authoredFrames[index];
            ValidateFrameBinding(
                mapSet,
                GetSourcePath(frame.action, direction, frame.frame),
                GetDirectionMapPath(frame.action, direction, frame.frame)
            );
        }

        Debug.Log("ActorCommonM02 authored TattooMap validation passed for direction " + direction + ": " + authoredFrames.Count + " frame(s).");
    }

    private static void ValidateStandardRegionLegend()
    {
        Texture2D map = AssetDatabase.LoadAssetAtPath<Texture2D>(LegendMapPath);
        Sprite overlay = AssetDatabase.LoadAssetAtPath<Sprite>(LegendOverlayPath);
        var importer = AssetImporter.GetAtPath(LegendMapPath) as TextureImporter;
        if (map == null || map.width != FramePixels || map.height != FramePixels || overlay == null || importer == null)
        {
            throw new InvalidOperationException("Standard six-region legend assets are missing or have an invalid size.");
        }

        if (importer.sRGBTexture || importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed || importer.filterMode != FilterMode.Point)
        {
            throw new InvalidOperationException("Standard six-region legend TattooMap importer is invalid.");
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RegionLegendScenePath) == null)
        {
            throw new InvalidOperationException("Standard six-region legend scene is missing.");
        }
    }

    private static void GenerateMaps(string[] sourcePaths, string reviewDirectory)
    {
        for (int index = 0; index < FrameCount; index++)
        {
            Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePaths[index]);
            if (source == null || source.width != FramePixels || source.height != FramePixels)
            {
                throw new InvalidOperationException("Expected a readable 512x512 source frame: " + sourcePaths[index]);
            }

            Color32[] sourcePixels = source.GetPixels32();
            Color32[] mapPixels = new Color32[sourcePixels.Length];
            Color32[] debugPixels = new Color32[sourcePixels.Length];
            WriteFrameMap(sourcePixels, mapPixels, debugPixels, GetEffectiveRegions(Action, Direction, index + 1, RollRightRegions[index]), GetSkinTolerance(Action, Direction, index + 1));

            string mapPath = MapDirectory + "/" + GetMapFileName(index + 1);
            WritePng(mapPath, mapPixels);
            string debugPath = Path.Combine(reviewDirectory, GetFrameName(index + 1).Replace(".png", "_tattoo_map_review.png"));
            WritePng(debugPath, debugPixels);
            string compositePath = Path.Combine(reviewDirectory, GetFrameName(index + 1).Replace(".png", "_tattoo_composite_preview.png"));
            WritePng(compositePath, BuildCompositePreview(sourcePixels, mapPixels));
        }
    }

    private static void GenerateManualRightMaps(string projectRoot)
    {
        for (int index = 0; index < ManualRightFrames.Length; index++)
        {
            ManualFrame frame = ManualRightFrames[index];
            string sourcePath = GetSourcePath(frame.Action, Direction, frame.Frame);
            Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
            if (source == null || source.width != FramePixels || source.height != FramePixels)
            {
                throw new InvalidOperationException("Expected a readable 512x512 manual right-facing source frame: " + sourcePath);
            }

            Color32[] sourcePixels = source.GetPixels32();
            var mapPixels = new Color32[sourcePixels.Length];
            var debugPixels = new Color32[sourcePixels.Length];
            WriteFrameMap(sourcePixels, mapPixels, debugPixels, GetEffectiveRegions(frame.Action, Direction, frame.Frame, frame.Regions), GetSkinTolerance(frame.Action, Direction, frame.Frame));
            string mapPath = GetRightMapPath(frame.Action, frame.Frame);
            WritePng(mapPath, mapPixels);

            string reviewDirectory = Path.Combine(projectRoot, ReviewRelativeDirectory, "right_actions", frame.Action);
            Directory.CreateDirectory(reviewDirectory);
            string baseName = GetFrameName(frame.Action, Direction, frame.Frame).Replace(".png", string.Empty);
            WritePng(Path.Combine(reviewDirectory, baseName + "_tattoo_map_review.png"), debugPixels);
            WritePng(Path.Combine(reviewDirectory, baseName + "_tattoo_composite_preview.png"), BuildCompositePreview(sourcePixels, mapPixels));
        }
    }

    private static void GenerateStandardRegionLegend(string projectRoot)
    {
        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(LegendSourcePath);
        if (source == null || source.width != FramePixels || source.height != FramePixels)
        {
            throw new InvalidOperationException("Expected a readable 512x512 standard-pose source frame: " + LegendSourcePath);
        }

        Color32[] sourcePixels = source.GetPixels32();
        var mapPixels = new Color32[sourcePixels.Length];
        var debugPixels = new Color32[sourcePixels.Length];
        WriteFrameMap(sourcePixels, mapPixels, debugPixels, IdleDownLegendRegions);
        WritePng(LegendMapPath, mapPixels);
        WritePng(LegendOverlayPath, BuildRegionOverlay(mapPixels));

        string reviewDirectory = Path.Combine(projectRoot, ReviewRelativeDirectory, "region_legend");
        Directory.CreateDirectory(reviewDirectory);
        WritePng(Path.Combine(reviewDirectory, "actor_common_m02_idle_down_01_six_region_legend.png"), debugPixels);
        WritePng(Path.Combine(reviewDirectory, "actor_common_m02_idle_down_01_six_region_pattern_preview.png"), BuildCompositePreview(sourcePixels, mapPixels));
    }

    private static void EnsureReadableSource(string sourcePath, List<string> changedImporterPaths)
    {
        var importer = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException("M02 source texture is missing: " + sourcePath);
        }

        if (!importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
            changedImporterPaths.Add(sourcePath);
        }
    }

    private static void WriteFrameMap(Color32[] sourcePixels, Color32[] mapPixels, Color32[] debugPixels, TattooRegion[] regions, float skinTolerance = 0f)
    {
        for (int imageY = 0; imageY < FramePixels; imageY++)
        {
            int textureY = FramePixels - 1 - imageY;
            for (int x = 0; x < FramePixels; x++)
            {
                int pixelIndex = textureY * FramePixels + x;
                Color32 source = sourcePixels[pixelIndex];
                Color32 displaySource = CompositeOnBlack(source);
                debugPixels[pixelIndex] = displaySource;
                if (!IsSkinPixel(source, skinTolerance))
                {
                    continue;
                }

                for (int regionIndex = 0; regionIndex < regions.Length; regionIndex++)
                {
                    TattooRegion region = regions[regionIndex];
                    if (!region.TryMap(x, imageY, out float u, out float v))
                    {
                        continue;
                    }

                    mapPixels[pixelIndex] = new Color32(ToByte(u), ToByte(v), (byte)region.PartId, 255);
                    debugPixels[pixelIndex] = Blend(displaySource, PartDebugColors[region.PartId - 1], 0.80f);
                    break;
                }
            }
        }
    }

    private static bool IsSkinPixel(Color32 color, float tolerance)
    {
        if (color.a < 16)
        {
            return false;
        }

        tolerance = Mathf.Clamp(tolerance, -0.12f, 0.12f);
        float red = color.r / 255f;
        float green = color.g / 255f;
        float blue = color.b / 255f;
        return red > 0.30f - tolerance &&
               green > 0.16f - tolerance &&
               red > green * (1.10f - tolerance * 0.5f) &&
               green > blue * (1.08f - tolerance * 0.4f) &&
               red - blue > 0.20f - tolerance;
    }

    // This is a review-only CPU equivalent of the shader's local-UV clipping rule. It renders all
    // approved skin pixels as continuous, contrasting, 80% opaque colour regions so an artist can
    // inspect assignment and clipping before a final pattern atlas exists. It is not in-game art.
    private static Color32[] BuildCompositePreview(Color32[] sourcePixels, Color32[] mapPixels)
    {
        var result = new Color32[sourcePixels.Length];
        for (int index = 0; index < sourcePixels.Length; index++)
        {
            Color32 source = sourcePixels[index];
            Color32 map = mapPixels[index];
            Color32 displaySource = CompositeOnBlack(source);
            if (source.a < 16 || map.a < 16 || map.b < 1 || map.b > 6)
            {
                result[index] = displaySource;
                continue;
            }

            result[index] = Blend(displaySource, PartDebugColors[map.b - 1], 0.80f);
        }

        return result;
    }

    private static Color32[] BuildRegionOverlay(Color32[] mapPixels)
    {
        var result = new Color32[mapPixels.Length];
        for (int index = 0; index < mapPixels.Length; index++)
        {
            Color32 map = mapPixels[index];
            if (map.a < 16 || map.b < 1 || map.b > TotemTattooService.PartCount)
            {
                continue;
            }

            Color32 color = PartDebugColors[map.b - 1];
            color.a = 175;
            result[index] = color;
        }

        return result;
    }

    private static float SampleAtlasPattern(int patternId, float u, float v)
    {
        // Review-only atlas: no symbols, rings, dots, or repeated motifs. The TattooMap alpha
        // remains the sole clip mask, so the region can never paint outside approved skin.
        return 1f;
    }

    private static void ConfigureMapImporters()
    {
        for (int index = 0; index < FrameCount; index++)
        {
            string mapPath = MapDirectory + "/" + GetMapFileName(index + 1);
            ConfigureTattooMapImporter(mapPath);
        }

        for (int index = 0; index < ManualRightFrames.Length; index++)
        {
            ConfigureTattooMapImporter(GetRightMapPath(ManualRightFrames[index].Action, ManualRightFrames[index].Frame));
        }

        ConfigureTattooMapImporter(LegendMapPath);

        var overlayImporter = AssetImporter.GetAtPath(LegendOverlayPath) as TextureImporter;
        if (overlayImporter == null)
        {
            throw new InvalidOperationException("Could not configure six-region overlay importer: " + LegendOverlayPath);
        }

        overlayImporter.textureType = TextureImporterType.Sprite;
        overlayImporter.spriteImportMode = SpriteImportMode.Single;
        var overlaySettings = new TextureImporterSettings();
        overlayImporter.ReadTextureSettings(overlaySettings);
        overlaySettings.spriteAlignment = (int)SpriteAlignment.Custom;
        overlaySettings.spritePivot = new Vector2(0.5f, 1f / FramePixels);
        overlayImporter.SetTextureSettings(overlaySettings);
        overlayImporter.spritePixelsPerUnit = FramePixels;
        overlayImporter.sRGBTexture = true;
        overlayImporter.alphaIsTransparency = true;
        overlayImporter.mipmapEnabled = false;
        overlayImporter.wrapMode = TextureWrapMode.Clamp;
        overlayImporter.filterMode = FilterMode.Bilinear;
        overlayImporter.textureCompression = TextureImporterCompression.Uncompressed;
        overlayImporter.maxTextureSize = FramePixels;
        overlayImporter.isReadable = false;
        overlayImporter.SaveAndReimport();
    }

    private static void ConfigureTattooMapImporter(string mapPath)
    {
        var importer = AssetImporter.GetAtPath(mapPath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException("Could not configure TattooMap importer: " + mapPath);
        }

        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = false;
        importer.alphaIsTransparency = false;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = FramePixels;
        importer.isReadable = false;
        importer.SaveAndReimport();
    }

    private static TotemTattooFrameMapSet CreateOrUpdateMapSet()
    {
        TotemTattooFrameMapSet mapSet = AssetDatabase.LoadAssetAtPath<TotemTattooFrameMapSet>(MapSetPath);
        if (mapSet == null)
        {
            // A previous prototype stored this type in a differently named source file. Unity
            // therefore created a missing-script asset at the same path; discard that invalid
            // generated artifact before recreating the now-valid ScriptableObject.
            if (AssetDatabase.LoadMainAssetAtPath(MapSetPath) != null && !AssetDatabase.DeleteAsset(MapSetPath))
            {
                throw new InvalidOperationException("Could not replace the invalid TattooMap set asset: " + MapSetPath);
            }

            mapSet = ScriptableObject.CreateInstance<TotemTattooFrameMapSet>();
            AssetDatabase.CreateAsset(mapSet, MapSetPath);
        }

        var bindings = new List<TotemTattooFrameBinding>(mapSet.GetBindings());
        for (int index = 0; index < FrameCount; index++)
        {
            string sourcePath = SpriteDirectory + "/" + GetFrameName(index + 1);
            string mapPath = MapDirectory + "/" + GetMapFileName(index + 1);
            ReplaceOrAddBinding(bindings, new TotemTattooFrameBinding
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(sourcePath),
                tattooMap = AssetDatabase.LoadAssetAtPath<Texture2D>(mapPath),
            });
        }

        for (int index = 0; index < ManualRightFrames.Length; index++)
        {
            ManualFrame frame = ManualRightFrames[index];
            string sourcePath = GetSourcePath(frame.Action, Direction, frame.Frame);
            ReplaceOrAddBinding(bindings, new TotemTattooFrameBinding
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(sourcePath),
                tattooMap = AssetDatabase.LoadAssetAtPath<Texture2D>(GetRightMapPath(frame.Action, frame.Frame)),
            });
        }

        // Write through SerializedObject rather than only the runtime setter. The map set lives in
        // the Hotfix assembly and must survive an AssetDatabase refresh/reload before the Player
        // can use it at runtime.
        var serializedMapSet = new SerializedObject(mapSet);
        SerializedProperty serializedBindings = serializedMapSet.FindProperty("bindings");
        if (serializedBindings == null)
        {
            throw new InvalidOperationException("TattooMap set does not expose its bindings field.");
        }

        serializedBindings.arraySize = bindings.Count;
        for (int index = 0; index < bindings.Count; index++)
        {
            SerializedProperty binding = serializedBindings.GetArrayElementAtIndex(index);
            binding.FindPropertyRelative("sprite").objectReferenceValue = bindings[index].sprite;
            binding.FindPropertyRelative("tattooMap").objectReferenceValue = bindings[index].tattooMap;
        }

        serializedMapSet.ApplyModifiedPropertiesWithoutUndo();
        mapSet.SetBindings(bindings.ToArray());
        EditorUtility.SetDirty(mapSet);
        return mapSet;
    }

    private static void ReplaceOrAddBinding(List<TotemTattooFrameBinding> bindings, TotemTattooFrameBinding value)
    {
        if (value.sprite == null || value.tattooMap == null)
        {
            throw new InvalidOperationException("TattooMap binding contains a missing sprite or map.");
        }

        for (int index = 0; index < bindings.Count; index++)
        {
            if (bindings[index].sprite == value.sprite)
            {
                bindings[index] = value;
                return;
            }
        }

        bindings.Add(value);
    }

    private static Texture2D CreateOrUpdatePrototypePatternAtlas()
    {
        const int cellsAcross = 4;
        const int cellsDown = 2;
        const int cellSize = 128;
        const int atlasWidth = cellsAcross * cellSize;
        const int atlasHeight = cellsDown * cellSize;
        var pixels = new Color32[atlasWidth * atlasHeight];
        for (int patternIndex = 0; patternIndex < 8; patternIndex++)
        {
            int cellX = patternIndex % cellsAcross;
            int cellY = patternIndex / cellsAcross;
            for (int y = 0; y < cellSize; y++)
            {
                for (int x = 0; x < cellSize; x++)
                {
                    float u = (x + 0.5f) / cellSize;
                    float v = (y + 0.5f) / cellSize;
                    float alpha = SampleAtlasPattern(patternIndex + 1, u, v);
                    int atlasX = cellX * cellSize + x;
                    int atlasY = cellY * cellSize + y;
                    pixels[atlasY * atlasWidth + atlasX] = new Color32(255, 255, 255, ToByte(alpha));
                }
            }
        }

        WritePng(PatternAtlasPath, atlasWidth, atlasHeight, pixels);
        AssetDatabase.ImportAsset(PatternAtlasPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(PatternAtlasPath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException("Could not configure Tattoo pattern atlas importer.");
        }

        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = false;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = atlasWidth;
        importer.isReadable = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(PatternAtlasPath);
    }

    private static Material CreateOrUpdateTattooMaterial(Texture2D patternAtlas)
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(TattooShaderPath);
        if (shader == null)
        {
            throw new InvalidOperationException("Tattoo sprite shader is missing: " + TattooShaderPath);
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "ActorCommonM02TattooPrototype" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        material.shader = shader;
        material.SetTexture("_TattooPatternAtlas", patternAtlas);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void BindPlayerPresenter(TotemTattooFrameMapSet mapSet, Material material, Texture2D patternAtlas)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            SpriteRenderer spriteRenderer = prefabRoot.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                throw new InvalidOperationException("Player prefab has no SpriteRenderer.");
            }

            TotemTattooVisualPresenter presenter = prefabRoot.GetComponent<TotemTattooVisualPresenter>();
            if (presenter == null)
            {
                presenter = prefabRoot.AddComponent<TotemTattooVisualPresenter>();
            }

            var serialized = new SerializedObject(presenter);
            SetObjectReference(serialized, "spriteRenderer", spriteRenderer);
            SetObjectReference(serialized, "tattooMaterial", material);
            SetObjectReference(serialized, "tattooPatternAtlas", patternAtlas);
            SetObjectReference(serialized, "frameMapSet", mapSet);
            EnsureDefaultPartPlacements(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void ValidatePlayerPresenter(TotemTattooFrameMapSet mapSet)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            TotemTattooVisualPresenter presenter = prefabRoot.GetComponent<TotemTattooVisualPresenter>();
            if (presenter == null)
            {
                throw new InvalidOperationException("Player prefab must contain TotemTattooVisualPresenter.");
            }

            var serialized = new SerializedObject(presenter);
            SerializedProperty material = serialized.FindProperty("tattooMaterial");
            SerializedProperty atlas = serialized.FindProperty("tattooPatternAtlas");
            SerializedProperty bindings = serialized.FindProperty("frameMapSet");
            SerializedProperty placements = serialized.FindProperty("partPlacements");
            if (material == null || material.objectReferenceValue == null || atlas == null || atlas.objectReferenceValue == null || bindings == null || bindings.objectReferenceValue != mapSet || placements == null || placements.arraySize != TotemTattooService.PartCount)
            {
                throw new InvalidOperationException("Player Tattoo presenter references are incomplete.");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void CreateShaderPreviewScene(TotemTattooFrameMapSet mapSet, Material material, Texture2D patternAtlas)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Tattoo Preview Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
        camera.orthographicSize = 2.2f;
        camera.transform.position = new Vector3(0f, 0f, -10f);

        var previewSprites = new Sprite[FrameCount];
        var previewMaterials = new Material[FrameCount];

        for (int index = 0; index < FrameCount; index++)
        {
            string sourcePath = SpriteDirectory + "/" + GetFrameName(index + 1);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(sourcePath);
            if (!mapSet.TryGetTattooMap(sprite, out Texture2D map))
            {
                throw new InvalidOperationException("Preview cannot resolve TattooMap for " + sourcePath);
            }

            var preview = new GameObject("RollRight_" + (index + 1).ToString("00"));
            preview.transform.position = new Vector3((index % 4 - 1.5f) * 1.06f, index < 4 ? 0.58f : -0.48f, 0f);
            var renderer = preview.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = CreateOrUpdatePreviewMaterial(index + 1, material, map, patternAtlas);
            renderer.sortingOrder = index;
            previewSprites[index] = sprite;
            previewMaterials[index] = renderer.sharedMaterial;
        }

        CreateAnimatedPreview(previewSprites, previewMaterials);

        EditorSceneManager.SaveScene(scene, ShaderPreviewScenePath);
    }

    private static void CreateAnimatedPreview(Sprite[] previewSprites, Material[] previewMaterials)
    {
        var animatedPreview = new GameObject("AnimatedRollRight");
        animatedPreview.transform.position = new Vector3(0f, -1.42f, 0f);
        animatedPreview.transform.localScale = Vector3.one * 1.25f;
        var renderer = animatedPreview.AddComponent<SpriteRenderer>();
        renderer.sprite = previewSprites[0];
        renderer.sharedMaterial = previewMaterials[0];
        renderer.sortingOrder = 100;
        var loop = animatedPreview.AddComponent<TotemTattooRollPreviewLoop>();
        var serialized = new SerializedObject(loop);
        SetObjectReference(serialized, "spriteRenderer", renderer);
        SetObjectReferenceArray(serialized, "frames", previewSprites);
        SetObjectReferenceArray(serialized, "frameMaterials", previewMaterials);
        SerializedProperty rate = serialized.FindProperty("framesPerSecond");
        if (rate == null)
        {
            throw new InvalidOperationException("Tattoo preview loop rate property is missing.");
        }

        rate.floatValue = 6f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateRightDirectionPreviewScene(TotemTattooFrameMapSet mapSet, Material material, Texture2D patternAtlas)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Right TattooMap Review Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
        camera.orthographicSize = 2.4f;
        camera.transform.position = new Vector3(0f, 0f, -10f);

        RightActionPreview[] actions =
        {
            new RightActionPreview("idle", 4),
            new RightActionPreview("walk", 6),
            new RightActionPreview("sprint", 6),
            new RightActionPreview("hit", 4),
            new RightActionPreview("attack", 6),
            new RightActionPreview("roll", 8),
            new RightActionPreview("death", 8),
        };

        for (int actionIndex = 0; actionIndex < actions.Length; actionIndex++)
        {
            RightActionPreview action = actions[actionIndex];
            var sprites = new Sprite[action.FrameCount];
            var materials = new Material[action.FrameCount];
            for (int frameIndex = 0; frameIndex < action.FrameCount; frameIndex++)
            {
                int frameNumber = frameIndex + 1;
                string sourcePath = GetSourcePath(action.Action, Direction, frameNumber);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(sourcePath);
                if (!mapSet.TryGetTattooMap(sprite, out Texture2D map))
                {
                    throw new InvalidOperationException("Right-direction preview cannot resolve TattooMap for " + sourcePath);
                }

                sprites[frameIndex] = sprite;
                materials[frameIndex] = CreateOrUpdatePreviewMaterial(action.Action, frameNumber, material, map, patternAtlas);
            }

            int column = actionIndex % 4;
            int row = actionIndex / 4;
            var preview = new GameObject("Animated" + char.ToUpperInvariant(action.Action[0]) + action.Action.Substring(1) + "Right");
            preview.transform.position = new Vector3((column - 1.5f) * 1.78f, row == 0 ? 0.68f : -1.67f, 0f);
            preview.transform.localScale = Vector3.one;
            var renderer = preview.AddComponent<SpriteRenderer>();
            renderer.sprite = sprites[0];
            renderer.sharedMaterial = materials[0];
            renderer.sortingOrder = actionIndex;
            var loop = preview.AddComponent<TotemTattooRollPreviewLoop>();
            var serialized = new SerializedObject(loop);
            SetObjectReference(serialized, "spriteRenderer", renderer);
            SetObjectReferenceArray(serialized, "frames", sprites);
            SetObjectReferenceArray(serialized, "frameMaterials", materials);
            SerializedProperty rate = serialized.FindProperty("framesPerSecond");
            if (rate == null)
            {
                throw new InvalidOperationException("Tattoo preview loop rate property is missing.");
            }

            rate.floatValue = action.Action == "death" ? 4f : 6f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            CreatePreviewLabel(action.Action.ToUpperInvariant(), preview.transform.position + new Vector3(-0.44f, -0.26f, 0f));
        }

        EditorSceneManager.SaveScene(scene, RightDirectionPreviewScenePath);
    }

    private static void CreatePreviewLabel(string text, Vector3 position)
    {
        var label = new GameObject("Label_" + text);
        label.transform.position = position;
        var textMesh = label.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.anchor = TextAnchor.MiddleLeft;
        textMesh.alignment = TextAlignment.Left;
        textMesh.fontSize = 34;
        textMesh.characterSize = 0.035f;
        textMesh.color = Color.white;
    }

    private static void CreateRegionLegendScene()
    {
        Sprite bodySprite = AssetDatabase.LoadAssetAtPath<Sprite>(LegendSourcePath);
        Sprite overlaySprite = AssetDatabase.LoadAssetAtPath<Sprite>(LegendOverlayPath);
        if (bodySprite == null || overlaySprite == null)
        {
            throw new InvalidOperationException("Six-region legend requires the idle sprite and its overlay.");
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Tattoo Region Legend Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
        camera.orthographicSize = 0.80f;
        camera.transform.position = new Vector3(0f, 0.5f, -10f);

        var body = new GameObject("StandardPose_IdleDown");
        body.transform.position = new Vector3(-0.28f, 0f, 0f);
        var bodyRenderer = body.AddComponent<SpriteRenderer>();
        bodyRenderer.sprite = bodySprite;
        bodyRenderer.sortingOrder = 1;

        var overlay = new GameObject("SixApprovedTattooRegions_Overlay");
        overlay.transform.position = body.transform.position;
        var overlayRenderer = overlay.AddComponent<SpriteRenderer>();
        overlayRenderer.sprite = overlaySprite;
        overlayRenderer.sortingOrder = 2;

        CreateLegendLabel("1  HEAD\nTemple / cheek", new Vector3(0.28f, 0.90f, 0f), PartDebugColors[0]);
        CreateLegendLabel("2  TORSO\nBare chest", new Vector3(0.28f, 0.68f, 0f), PartDebugColors[1]);
        CreateLegendLabel("3  LEFT ARM\nOuter upper arm", new Vector3(-1.32f, 0.64f, 0f), PartDebugColors[2]);
        CreateLegendLabel("4  RIGHT ARM\nOuter upper arm", new Vector3(0.28f, 0.52f, 0f), PartDebugColors[3]);
        CreateLegendLabel("5  LEFT LEG\nOuter thigh", new Vector3(-1.32f, 0.24f, 0f), PartDebugColors[4]);
        CreateLegendLabel("6  RIGHT LEG\nOuter thigh", new Vector3(0.28f, 0.25f, 0f), PartDebugColors[5]);

        EditorSceneManager.SaveScene(scene, RegionLegendScenePath);
    }

    private static void CreateLegendLabel(string text, Vector3 position, Color32 color)
    {
        var label = new GameObject("Label_" + text.Replace('\n', '_').Replace(' ', '_'));
        label.transform.position = position;
        var textMesh = label.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.anchor = TextAnchor.MiddleLeft;
        textMesh.alignment = TextAlignment.Left;
        textMesh.fontSize = 34;
        textMesh.characterSize = 0.024f;
        textMesh.color = color;
    }

    private static Material CreateOrUpdatePreviewMaterial(int frameNumber, Material prototypeMaterial, Texture2D tattooMap, Texture2D patternAtlas)
    {
        return CreateOrUpdatePreviewMaterial(Action, frameNumber, prototypeMaterial, tattooMap, patternAtlas);
    }

    private static Material CreateOrUpdatePreviewMaterial(string action, int frameNumber, Material prototypeMaterial, Texture2D tattooMap, Texture2D patternAtlas)
    {
        // These are actual assets rather than runtime clones: the review scene must remain valid
        // after it is closed and reopened, so each frame needs its own persisted map binding.
        string actionName = char.ToUpperInvariant(action[0]) + action.Substring(1);
        string materialPath = PreviewMaterialDirectory + "/ActorCommonM02" + actionName + "Right_" + frameNumber.ToString("00") + "_TattooPreview.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(prototypeMaterial);
            AssetDatabase.CreateAsset(material, materialPath);
        }

        material.SetTexture("_TattooMap", tattooMap);
        material.SetTexture("_TattooPatternAtlas", patternAtlas);
        material.SetVector("_TattooPart1", new Vector4(0.79f, 0.24f, 0.22f, 1f));
        material.SetVector("_TattooPart2", new Vector4(0.83f, 0.65f, 0.17f, 2f));
        material.SetVector("_TattooPart3", new Vector4(0.27f, 0.60f, 0.38f, 3f));
        material.SetVector("_TattooPart4", new Vector4(0.24f, 0.47f, 0.71f, 4f));
        material.SetVector("_TattooPart5", new Vector4(0.49f, 0.30f, 0.60f, 5f));
        material.SetVector("_TattooPart6", new Vector4(0.79f, 0.57f, 0.20f, 6f));
        for (int index = 1; index <= TotemTattooService.PartCount; index++)
        {
            material.SetVector("_TattooTransform" + index, new Vector4(0.5f, 0.5f, 1f, 0f));
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void SetObjectReference(SerializedObject serialized, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            throw new InvalidOperationException("Tattoo presenter property is missing: " + propertyName);
        }

        property.objectReferenceValue = value;
    }

    private static void SetObjectReferenceArray(SerializedObject serialized, string propertyName, UnityEngine.Object[] values)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || !property.isArray)
        {
            throw new InvalidOperationException("Tattoo preview property is missing or not an array: " + propertyName);
        }

        property.arraySize = values.Length;
        for (int index = 0; index < values.Length; index++)
        {
            property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }
    }

    private static void EnsureDefaultPartPlacements(SerializedObject serialized)
    {
        SerializedProperty placements = serialized.FindProperty("partPlacements");
        if (placements == null)
        {
            throw new InvalidOperationException("Tattoo presenter placement property is missing.");
        }

        placements.arraySize = TotemTattooService.PartCount;
        for (int index = 0; index < TotemTattooService.PartCount; index++)
        {
            SerializedProperty item = placements.GetArrayElementAtIndex(index);
            item.FindPropertyRelative("partId").intValue = index + 1;
            SerializedProperty placement = item.FindPropertyRelative("placement");
            placement.FindPropertyRelative("offset").vector2Value = new Vector2(0.5f, 0.5f);
            placement.FindPropertyRelative("scale").floatValue = 1f;
        }
    }

    private static void WritePng(string destination, Color32[] pixels)
    {
        WritePng(destination, FramePixels, FramePixels, pixels);
    }

    private static void WritePng(string destination, int width, int height, Color32[] pixels)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        File.WriteAllBytes(Path.GetFullPath(destination), texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
    }

    // Hand-authored data overrides only the selected semantic part. This lets an artist correct
    // one arm or leg without losing the remaining conservative fallback regions for that frame.
    private static TattooRegion[] GetEffectiveRegions(string action, string direction, int frame, TattooRegion[] fallback)
    {
        TattooMapRegionAuthoringAsset authoring = AssetDatabase.LoadAssetAtPath<TattooMapRegionAuthoringAsset>(AuthoringAssetPath);
        TattooMapFrameAuthoring authoredFrame = authoring?.FindFrame(action, direction, frame);
        if (authoredFrame == null || authoredFrame.regions == null || authoredFrame.regions.Count == 0)
        {
            return fallback;
        }

        var result = new List<TattooRegion>(fallback);
        for (int index = 0; index < authoredFrame.regions.Count; index++)
        {
            TattooMapRegionAuthoring authored = authoredFrame.regions[index];
            if (authored == null || authored.partId < 1 || authored.partId > TotemTattooService.PartCount)
            {
                continue;
            }

            for (int regionIndex = result.Count - 1; regionIndex >= 0; regionIndex--)
            {
                if (result[regionIndex].PartId == authored.partId)
                {
                    result.RemoveAt(regionIndex);
                }
            }

            Vector2[] corners = TattooMapRegionAuthoringGeometry.GetCorners(authored);
            result.Add(new TattooRegion(authored.partId, GetZoneForPartId(authored.partId), corners[0], corners[1], corners[2], corners[3]));
        }

        return result.ToArray();
    }

    private static float GetSkinTolerance(string action, string direction, int frame)
    {
        TattooMapRegionAuthoringAsset authoring = AssetDatabase.LoadAssetAtPath<TattooMapRegionAuthoringAsset>(AuthoringAssetPath);
        TattooMapFrameAuthoring authoredFrame = authoring?.FindFrame(action, direction, frame);
        return Mathf.Clamp(authoredFrame?.skinTolerance ?? 0f, -0.12f, 0.12f);
    }

    private static TattooZone GetZoneForPartId(int partId)
    {
        switch (partId)
        {
            case 1: return TattooZone.HeadTemple;
            case 2: return TattooZone.TorsoChest;
            case 3: return TattooZone.LeftArmOuter;
            case 4: return TattooZone.RightArmOuter;
            case 5: return TattooZone.LeftLegOuterThigh;
            case 6: return TattooZone.RightLegOuterThigh;
            default: throw new ArgumentOutOfRangeException(nameof(partId), partId, "Tattoo part id must be between 1 and 6.");
        }
    }

    private static void EnsureFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder))
        {
            return;
        }

        string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
        string folderName = Path.GetFileName(assetFolder);
        if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(folderName) || !AssetDatabase.IsValidFolder(parent))
        {
            throw new InvalidOperationException("Cannot create TattooMap folder because its parent is missing: " + assetFolder);
        }

        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static TattooRegion Region(int partId, TattooZone zone, int x0, int y0, int x1, int y1, int x2, int y2, int x3, int y3)
    {
        return new TattooRegion(partId, zone, new Vector2(x0, y0), new Vector2(x1, y1), new Vector2(x2, y2), new Vector2(x3, y3));
    }

    private static ManualFrame Frame(string action, int frame, params TattooRegion[] regions)
    {
        return new ManualFrame(action, frame, regions);
    }

    private static TattooRegion Box(int partId, TattooZone zone, int left, int top, int right, int bottom)
    {
        return Region(partId, zone, left, top, right, top, right, bottom, left, bottom);
    }

    private static string GetSourcePath(string action, string direction, int frame)
    {
        return SpriteDirectory + "/" + GetFrameName(action, direction, frame);
    }

    private static string GetRightActionDirectory(string action)
    {
        return RightMapDirectory + "/" + action;
    }

    private static string GetDirectionActionDirectory(string direction, string action)
    {
        return SpriteDirectory + "/TattooMaps/" + direction + "/" + action;
    }

    private static string GetRightMapPath(string action, int frame)
    {
        return GetRightActionDirectory(action) + "/" + GetFrameName(action, Direction, frame).Replace(".png", "_tattoo_map.png");
    }

    private static string GetDirectionMapPath(string action, string direction, int frame)
    {
        return GetDirectionActionDirectory(direction, action) + "/" + GetFrameName(action, direction, frame).Replace(".png", "_tattoo_map.png");
    }

    private static string GetFrameName(int frame)
    {
        return GetFrameName(Action, Direction, frame);
    }

    private static string GetFrameName(string action, string direction, int frame)
    {
        return $"actor_common_m02_{action}_{direction}_{frame:00}.png";
    }

    private static string GetMapFileName(int frame)
    {
        return $"actor_common_m02_{Action}_{Direction}_{frame:00}_tattoo_map.png";
    }

    private static byte ToByte(float value)
    {
        return (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(value) * 255f), 0, 255);
    }

    private static Color32 Blend(Color32 source, Color32 tint, float amount)
    {
        return new Color32(
            (byte)Mathf.Lerp(source.r, tint.r, amount),
            (byte)Mathf.Lerp(source.g, tint.g, amount),
            (byte)Mathf.Lerp(source.b, tint.b, amount),
            255);
    }

    private static Color32 CompositeOnBlack(Color32 source)
    {
        float alpha = source.a / 255f;
        return new Color32(
            (byte)Mathf.RoundToInt(source.r * alpha),
            (byte)Mathf.RoundToInt(source.g * alpha),
            (byte)Mathf.RoundToInt(source.b * alpha),
            255);
    }

    private enum TattooZone
    {
        HeadTemple,
        TorsoChest,
        LeftArmOuter,
        RightArmOuter,
        LeftLegOuterThigh,
        RightLegOuterThigh,
    }

    private sealed class ManualFrame
    {
        public readonly string Action;
        public readonly int Frame;
        public readonly TattooRegion[] Regions;

        public ManualFrame(string action, int frame, TattooRegion[] regions)
        {
            Action = action;
            Frame = frame;
            Regions = regions;
        }
    }

    private readonly struct RightActionPreview
    {
        public readonly string Action;
        public readonly int FrameCount;

        public RightActionPreview(string action, int frameCount)
        {
            Action = action;
            FrameCount = frameCount;
        }
    }

    private readonly struct TattooRegion
    {
        public readonly int PartId;
        public readonly TattooZone Zone;
        public readonly Vector2 UpperOuter;
        public readonly Vector2 UpperInner;
        public readonly Vector2 LowerInner;
        public readonly Vector2 LowerOuter;

        public TattooRegion(int partId, TattooZone zone, Vector2 upperOuter, Vector2 upperInner, Vector2 lowerInner, Vector2 lowerOuter)
        {
            PartId = partId;
            Zone = zone;
            UpperOuter = upperOuter;
            UpperInner = upperInner;
            LowerInner = lowerInner;
            LowerOuter = lowerOuter;
        }

        public bool TryMap(float x, float y, out float u, out float v)
        {
            Vector2 point = new Vector2(x, y);
            if (TryGetBarycentric(point, UpperOuter, UpperInner, LowerInner, out Vector3 first))
            {
                u = first.y + first.z;
                v = first.z;
                return true;
            }

            if (TryGetBarycentric(point, UpperOuter, LowerInner, LowerOuter, out Vector3 second))
            {
                u = second.y;
                v = second.y + second.z;
                return true;
            }

            u = 0f;
            v = 0f;
            return false;
        }

        private static bool TryGetBarycentric(Vector2 point, Vector2 a, Vector2 b, Vector2 c, out Vector3 barycentric)
        {
            float denominator = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
            if (Mathf.Abs(denominator) < 0.0001f)
            {
                barycentric = Vector3.zero;
                return false;
            }

            float first = ((b.y - c.y) * (point.x - c.x) + (c.x - b.x) * (point.y - c.y)) / denominator;
            float second = ((c.y - a.y) * (point.x - c.x) + (a.x - c.x) * (point.y - c.y)) / denominator;
            float third = 1f - first - second;
            barycentric = new Vector3(first, second, third);
            const float edgeEpsilon = -0.0001f;
            return first >= edgeEpsilon && second >= edgeEpsilon && third >= edgeEpsilon;
        }
    }
}
#endif
