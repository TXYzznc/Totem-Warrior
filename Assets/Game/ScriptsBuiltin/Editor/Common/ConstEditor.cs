#if UNITY_EDITOR
using System.IO;
using UnityEngine;
namespace UGF.EditorTools
{

    /// <summary>
    /// 默认编辑器配置项
    /// </summary>
    public class ConstEditor
    {
        public const bool AutoScriptUTF8 = true;//新建脚本时自动修改脚本编码方式为utf-8以支持中文
        /// <summary>
        /// 打包资源前是否自动解决AB包重复依赖
        /// </summary>
        public const bool ResolveDuplicateAssets = true;
        public const string UIViewScriptFile = "Assets/Game/Scripts/UI/Core/UIViews.cs";
        public const string UISerializeFieldDir = "Assets/Game/Scripts/UI/UIVariables";//生成UI变量代码目录
        public const string UIItemSerializeFiledDir = "Assets/Game/Scripts/UI/UIItemVariables";
        public const string UITableExcel = "Core/UITable.xlsx";
        public static string UITableExcelFullPath => UtilityBuiltin.AssetsPath.GetCombinePath(DataTableExcelPath, UITableExcel);

        public const string EntityGroupTableExcel = "Core/EntityGroupTable.xlsx";
        public static string EntityGroupTableExcelFullPath => UtilityBuiltin.AssetsPath.GetCombinePath(DataTableExcelPath, EntityGroupTableExcel);

        public const string SoundGroupTableExcel = "Core/SoundGroupTable.xlsx";
        public static string SoundGroupTableExcelFullPath => UtilityBuiltin.AssetsPath.GetCombinePath(DataTableExcelPath, SoundGroupTableExcel);

        public const string UIGroupTableExcel = "Core/UIGroupTable.xlsx";
        public static string UIGroupTableExcelFullPath => UtilityBuiltin.AssetsPath.GetCombinePath(DataTableExcelPath, UIGroupTableExcel);

        public const string ConstGroupScriptFileFullName = "Assets/Game/Scripts/Common/Core/Const.Groups.cs";

        public static readonly string PrefabsPath = "Assets/Game/Prefabs";
        public static readonly string ScenePath = "Assets/Game/Scene";

        public const string DataTableCodeTemplate = "Assets/Game/ScriptsBuiltin/Editor/DataTableGenerator/DataTableCodeTemplate/DataTableCodeTemplate.txt"; //生成配置表代码的模板文件
        public const string BuiltinAssembly = "Assets/Game/ScriptsBuiltin/Runtime/Builtin.Runtime.asmdef";
        public const string HotfixAssembly = "Assets/Game/Scripts/Hotfix.asmdef";


        public const string SharedAssetBundleName = "SharedAssets";//AssetBundle分包共用资源
        internal static readonly string KeystorePass = "topgames";
        internal static readonly string KeyAliasPass = "topgames";
        internal static string KeystoreName => UtilityBuiltin.AssetsPath.GetCombinePath(Directory.GetParent(Application.dataPath).FullName, "user.keystore");
        internal static readonly string KeyAliasName = "release";
        internal static string AssetBundleOutputPath => UtilityBuiltin.AssetsPath.GetCombinePath(Directory.GetParent(Application.dataPath).FullName, "AB");
        public static readonly string UpdatePrefixUri = "http://127.0.0.1/1_0_0_1/";//默认资源下载地址
        internal static readonly string AppUpdateUrl = "https://play.google.com/store/apps/details?id=";

        /// <summary>
        /// 数据表Excel目录
        /// </summary>
        public static string DataTableExcelPath => UtilityBuiltin.AssetsPath.GetCombinePath(Directory.GetParent(Application.dataPath).FullName, "GameData/DataTables");
        /// <summary>
        /// 配置表Excel目录
        /// </summary>
        public static string ConfigExcelPath => UtilityBuiltin.AssetsPath.GetCombinePath(Directory.GetParent(Application.dataPath).FullName, "GameData/Configs");
        /// <summary>
        /// 语言国际化Excel目录
        /// </summary>
        public static string LanguageExcelPath => UtilityBuiltin.AssetsPath.GetCombinePath(Directory.GetParent(Application.dataPath).FullName, "GameData/Languages");

        public static string ToolsPath = UtilityBuiltin.AssetsPath.GetCombinePath(Directory.GetParent(Application.dataPath).FullName, "Tools");
        public static string AIDataPath => UtilityBuiltin.AssetsPath.GetCombinePath(Directory.GetParent(Application.dataPath).FullName, "GameData/AIData");
        public static string AIDataTablePath => UtilityBuiltin.AssetsPath.GetCombinePath(AIDataPath, "DataTables");
        public static string AIDataReportPath => UtilityBuiltin.AssetsPath.GetCombinePath(AIDataPath, "Reports");
        public static string DiagnosticReportPath => UtilityBuiltin.AssetsPath.GetCombinePath(Directory.GetParent(Application.dataPath).FullName, "GameData/Diagnostics/Reports");
        public const string DataTablePath = "Assets/Game/DataTable";
        public const string GameConfigPath = "Assets/Game/Config";
        public const string LanguagePath = "Assets/Game/Language";
        public const string DataTableCodePath = "Assets/Game/Scripts/DataTable";
        public const string UIScriptsPath = "Assets/Game/Scripts/UI";
        public const string UIItemScriptsPath = "Assets/Game/Scripts/UI/Item";
        public const string UIFormTemplate = "Assets/Game/ScriptsBuiltin/Editor/UI/Templates/UIFormTemplate.prefab";
        public const string UIDialogTemplate = "Assets/Game/ScriptsBuiltin/Editor/UI/Templates/UIDialogTemplate.prefab";
        public const string UIItemTemplate = "Assets/Game/ScriptsBuiltin/Editor/UI/Templates/UIItemTemplate.prefab";
        public const string UIScriptFileTemplate = "Assets/Game/ScriptsBuiltin/Editor/UI/Templates/UIScriptFileTemplate.txt";
        public const string UIItemScriptFileTemplate = "Assets/Game/ScriptsBuiltin/Editor/UI/Templates/UIItemScriptFileTemplate.txt";
    }
}
#endif
