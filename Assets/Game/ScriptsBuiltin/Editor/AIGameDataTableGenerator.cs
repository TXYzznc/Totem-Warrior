#if UNITY_EDITOR
using GameFramework.Editor.DataTableTools;
using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    public static class AIGameDataTableGenerator
    {
        private const int SchemaVersion = 1;
        private const string ManifestKind = "GF_X.DataTable.AI";
        private const string RoleComment = "comment";
        private const string RoleId = "id";
        private const string RoleData = "data";

        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        [MenuItem("Game Framework/GameTools/AI Data/Export DataTables Json", false, 1011)]
        public static void ExportAllDataTablesToAIJson()
        {
            var excelFiles = GameDataGenerator.GetAllGameDataExcels(GameDataType.DataTable, GameDataExcelFileType.MainFile | GameDataExcelFileType.ABTestFile);
            var report = ExportDataTablesToAIJson(excelFiles);
            WriteReport(report);
            AssetDatabase.Refresh();
        }

        [MenuItem("Game Framework/GameTools/AI Data/Validate DataTables Json", false, 1012)]
        public static void ValidateAllDataTablesAIJson()
        {
            var report = ImportDataTablesFromAIJson(GetAllAIJsonFiles(), syncExcel: false, writeGeneratedFiles: false);
            WriteReport(report);
            AssetDatabase.Refresh();
        }

        [MenuItem("Game Framework/GameTools/AI Data/Import DataTables Json", false, 1013)]
        public static void ImportAllDataTablesFromAIJson()
        {
            var report = ImportDataTablesFromAIJson(GetAllAIJsonFiles(), syncExcel: true, writeGeneratedFiles: true);
            WriteReport(report);
            AssetDatabase.Refresh();
        }

        [MenuItem("Game Framework/GameTools/AI Data/Reverse DataTables Json To Excel", false, 1014)]
        public static void ReverseAllDataTablesJsonToExcelMenu()
        {
            ReverseAllDataTablesJsonToExcel();
        }

        [MenuItem("Game Framework/GameTools/AI Data/Reverse Business DataTables Json To Excel", false, 1015)]
        public static void ReverseBusinessDataTablesJsonToExcelMenu()
        {
            ReverseBusinessDataTablesJsonToExcel();
        }

        [MenuItem("Game Framework/GameTools/AI Data/Check Business Json Excel Sync", false, 1016)]
        public static void CheckBusinessDataTablesJsonExcelSyncMenu()
        {
            var report = CheckBusinessDataTablesJsonExcelSync();
            WriteReport(report);
            AssetDatabase.Refresh();
        }

        public static AIDataTableReport ReverseAllDataTablesJsonToExcel()
        {
            var report = ImportDataTablesFromAIJson(GetAllAIJsonFiles(), syncExcel: true, writeGeneratedFiles: false);
            WriteReport(report);
            AssetDatabase.Refresh();
            return report;
        }

        public static AIDataTableReport ReverseBusinessDataTablesJsonToExcel()
        {
            var jsonFiles = GetAllAIJsonFiles()
                .Where(IsBusinessAIJsonFile)
                .ToArray();
            var report = ImportDataTablesFromAIJson(jsonFiles, syncExcel: true, writeGeneratedFiles: false);
            WriteReport(report);
            AssetDatabase.Refresh();
            return report;
        }

        public static AIDataTableReport ReverseDataTablesJsonToExcelByRelativePaths(IList<string> relativePaths)
        {
            var jsonFiles = GetAIJsonFilesByRelativePaths(relativePaths, out var missingFiles);
            var report = ImportDataTablesFromAIJson(jsonFiles, syncExcel: true, writeGeneratedFiles: false);
            foreach (var missingFile in missingFiles)
            {
                report.warnings.Add($"AI json file does not exist: {missingFile}");
            }

            report.RefreshSummary();
            WriteReport(report);
            AssetDatabase.Refresh();
            return report;
        }

        public static AIDataTableReport CheckBusinessDataTablesJsonExcelSync()
        {
            var jsonFiles = GetAllAIJsonFiles()
                .Where(IsBusinessAIJsonFile)
                .ToArray();
            var report = new AIDataTableReport("check-business-data-tables-json-excel-sync");
            if (jsonFiles.Length == 0)
            {
                report.warnings.Add("No Business AI data table json files found.");
                report.RefreshSummary();
                return report;
            }

            for (int i = 0; i < jsonFiles.Length; i++)
            {
                string jsonFile = jsonFiles[i];
                var item = report.AddItem(null, jsonFile);
                EditorUtility.DisplayProgressBar(report.action, jsonFile, i / (float)jsonFiles.Length);
                try
                {
                    if (!TryBuildRowsFromAIJson(jsonFile, item, out string relativePath, out var rows, out _))
                    {
                        continue;
                    }

                    string excelFile = GameDataGenerator.GameDataExcelRelative2FullPath(GameDataType.DataTable, relativePath);
                    item.tableName = relativePath;
                    item.sourceFile = excelFile;
                    if (string.IsNullOrWhiteSpace(excelFile) || !File.Exists(excelFile))
                    {
                        item.errors.Add($"Excel file does not exist: {excelFile}");
                        continue;
                    }

                    var diff = CalculateExcelDiff(excelFile, rows);
                    item.changedCellCount = diff.changedCellCount;
                    item.changedRowCount = diff.changedRowCount;
                    item.oldRowCount = diff.oldRowCount;
                    item.newRowCount = diff.newRowCount;
                    if (diff.changedCellCount > 0)
                    {
                        item.errors.Add($"Business xlsx is not synchronized with AI json. ChangedCells={diff.changedCellCount}, ChangedRows={diff.changedRowCount}. FirstMismatch={diff.firstMismatch}");
                        continue;
                    }

                    item.success = true;
                }
                catch (Exception exception)
                {
                    item.errors.Add(exception.ToString());
                }
            }

            EditorUtility.ClearProgressBar();
            report.RefreshSummary();
            return report;
        }

        public static AIDataTableReport ExportDataTablesToAIJson(IList<string> excelFiles)
        {
            var report = new AIDataTableReport("export-data-tables-json");
            if (excelFiles == null || excelFiles.Count == 0)
            {
                report.warnings.Add("No data table Excel files found.");
                return report;
            }

            for (int i = 0; i < excelFiles.Count; i++)
            {
                string excelFile = excelFiles[i];
                var item = report.AddItem(excelFile, null);
                EditorUtility.DisplayProgressBar("Export AI DataTable Json", excelFile, i / (float)excelFiles.Count);
                try
                {
                    if (!TryCreateManifestFromExcel(excelFile, item, out var manifest))
                    {
                        continue;
                    }

                    string jsonFile = GetAIJsonFile(manifest.relativePath);
                    item.tableName = manifest.relativePath;
                    item.jsonFile = jsonFile;
                    EnsureFileDirectory(jsonFile);
                    File.WriteAllText(jsonFile, JsonConvert.SerializeObject(manifest, Formatting.Indented), Utf8NoBom);
                    item.outputFile = jsonFile;
                    item.success = true;
                }
                catch (Exception exception)
                {
                    item.errors.Add(exception.ToString());
                }
            }

            EditorUtility.ClearProgressBar();
            report.RefreshSummary();
            return report;
        }

        public static AIDataTableReport ImportDataTablesFromAIJson(IList<string> jsonFiles, bool syncExcel, bool writeGeneratedFiles)
        {
            string action = writeGeneratedFiles ? "import-data-tables-json" : syncExcel ? "reverse-data-tables-json-to-excel" : "validate-data-tables-json";
            var report = new AIDataTableReport(action);
            if (jsonFiles == null || jsonFiles.Count == 0)
            {
                report.warnings.Add("No AI data table json files found.");
                return report;
            }

            var importedRelativePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < jsonFiles.Count; i++)
            {
                string jsonFile = jsonFiles[i];
                var item = report.AddItem(null, jsonFile);
                EditorUtility.DisplayProgressBar(report.action, jsonFile, i / (float)jsonFiles.Count);
                try
                {
                    if (!TryImportAIJson(jsonFile, syncExcel, writeGeneratedFiles, item, out string relativePath))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(relativePath))
                    {
                        importedRelativePaths.Add(relativePath);
                    }

                    item.success = true;
                }
                catch (Exception exception)
                {
                    item.errors.Add(exception.ToString());
                }
            }

            EditorUtility.ClearProgressBar();

            if (writeGeneratedFiles)
            {
                RefreshSpecialGeneratedScripts(importedRelativePaths);
            }

            report.RefreshSummary();
            return report;
        }

        private static bool TryCreateManifestFromExcel(string excelFile, AIDataTableReportItem item, out AIDataTableManifest manifest)
        {
            manifest = null;
            if (string.IsNullOrWhiteSpace(excelFile) || !File.Exists(excelFile))
            {
                item.errors.Add($"Excel file does not exist: {excelFile}");
                return false;
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            string tempExcelFile = GetTempExcelFile(excelFile);
            try
            {
                File.Copy(excelFile, tempExcelFile, true);
                using (var excelPackage = new ExcelPackage(tempExcelFile))
                {
                    if (excelPackage.Workbook.Worksheets.Count <= 0)
                    {
                        item.errors.Add($"Excel has no worksheets: {excelFile}");
                        return false;
                    }

                    var sheet = excelPackage.Workbook.Worksheets[0];
                    if (sheet.Dimension == null || sheet.Dimension.End.Row < 4 || sheet.Dimension.End.Column < 2)
                    {
                        item.errors.Add($"Excel format is invalid: {excelFile}");
                        return false;
                    }

                    string relativePath = GameDataGenerator.GetGameDataExcelRelativePath(GameDataType.DataTable, excelFile);
                    manifest = new AIDataTableManifest
                    {
                        schemaVersion = SchemaVersion,
                        kind = ManifestKind,
                        tableName = Path.GetFileNameWithoutExtension(excelFile),
                        relativePath = relativePath,
                        sourceExcel = ToProjectRelativePath(excelFile),
                        sourceExcelLastWriteUtc = File.GetLastWriteTimeUtc(excelFile).ToString("O"),
                        generatedAtUtc = DateTime.UtcNow.ToString("O"),
                        tableComment = GetExcelCellString(sheet, 1, 2),
                    };

                    int endColumn = sheet.Dimension.End.Column;
                    for (int column = 1; column <= endColumn; column++)
                    {
                        string name = GetExcelCellString(sheet, 2, column);
                        string type = GetExcelCellString(sheet, 3, column);
                        string comment = GetExcelCellString(sheet, 4, column);
                        string role = GetColumnRole(column, name, type);
                        manifest.columns.Add(new AIDataColumn
                        {
                            index = column,
                            key = CreateColumnKey(column, role, name),
                            name = name,
                            type = type,
                            comment = comment,
                            role = role,
                        });
                    }

                    for (int row = 5; row <= sheet.Dimension.End.Row; row++)
                    {
                        var cells = new List<string>(endColumn);
                        for (int column = 1; column <= endColumn; column++)
                        {
                            cells.Add(GetExcelCellString(sheet, row, column));
                        }

                        if (IsBlankRow(cells))
                        {
                            continue;
                        }

                        manifest.rows.Add(CreateDataRow(row, cells, manifest.columns));
                    }
                }
            }
            finally
            {
                if (File.Exists(tempExcelFile))
                {
                    File.Delete(tempExcelFile);
                }
            }

            return true;
        }

        private static bool TryImportAIJson(string jsonFile, bool syncExcel, bool writeGeneratedFiles, AIDataTableReportItem item, out string relativePath)
        {
            if (!TryBuildRowsFromAIJson(jsonFile, item, out relativePath, out var rows, out var manifest))
            {
                return false;
            }

            string excelFile = GameDataGenerator.GameDataExcelRelative2FullPath(GameDataType.DataTable, relativePath);
            if (File.Exists(excelFile) && File.GetLastWriteTimeUtc(excelFile) > File.GetLastWriteTimeUtc(jsonFile))
            {
                item.warnings.Add($"Excel is newer than AI json. Import will still use json: {excelFile}");
            }

            string outputTxtFile;
            if (writeGeneratedFiles)
            {
                outputTxtFile = GameDataGenerator.GetGameDataExcelOutputFile(GameDataType.DataTable, excelFile);
                EnsureFileDirectory(outputTxtFile);
                File.WriteAllText(outputTxtFile, RowsToText(rows), Utf8NoBom);
                item.outputFile = outputTxtFile;
            }
            else
            {
                outputTxtFile = UtilityBuiltin.AssetsPath.GetCombinePath(GetValidateTempPath(), relativePath + ".txt");
                EnsureFileDirectory(outputTxtFile);
                File.WriteAllText(outputTxtFile, RowsToText(rows), Utf8NoBom);
                item.outputFile = outputTxtFile;
            }

            if (!TryValidateAndGenerateDataTable(outputTxtFile, relativePath, writeGeneratedFiles, item))
            {
                return false;
            }

            if (syncExcel)
            {
                WriteExcelFileWithBackup(excelFile, relativePath, rows, item);
                RefreshManifestAfterExcelSync(jsonFile, manifest, excelFile, item);
                item.sourceFile = excelFile;
            }

            if (!writeGeneratedFiles && File.Exists(outputTxtFile))
            {
                File.Delete(outputTxtFile);
            }

            return true;
        }

        private static bool TryBuildRowsFromAIJson(string jsonFile, AIDataTableReportItem item, out string relativePath, out List<string[]> rows, out AIDataTableManifest manifest)
        {
            relativePath = null;
            rows = null;
            manifest = null;
            if (!File.Exists(jsonFile))
            {
                item.errors.Add($"Json file does not exist: {jsonFile}");
                return false;
            }

            item.jsonFile = jsonFile;
            manifest = JsonConvert.DeserializeObject<AIDataTableManifest>(File.ReadAllText(jsonFile, Utf8NoBom));
            if (manifest == null)
            {
                item.errors.Add($"Json content is empty: {jsonFile}");
                return false;
            }

            relativePath = GetManifestRelativePath(manifest, jsonFile);
            item.tableName = relativePath;
            if (!ValidateManifest(manifest, jsonFile, item))
            {
                return false;
            }

            rows = BuildTableRows(manifest, item, normalizeCustomJson: true);
            return item.errors.Count == 0;
        }

        private static void RefreshManifestAfterExcelSync(string jsonFile, AIDataTableManifest manifest, string excelFile, AIDataTableReportItem item)
        {
            if (manifest == null || string.IsNullOrWhiteSpace(jsonFile) || string.IsNullOrWhiteSpace(excelFile) || !File.Exists(excelFile))
            {
                return;
            }

            manifest.sourceExcel = ToProjectRelativePath(excelFile);
            manifest.sourceExcelLastWriteUtc = File.GetLastWriteTimeUtc(excelFile).ToString("O");
            manifest.generatedAtUtc = DateTime.UtcNow.ToString("O");
            File.WriteAllText(jsonFile, JsonConvert.SerializeObject(manifest, Formatting.Indented), Utf8NoBom);
            File.SetLastWriteTimeUtc(jsonFile, DateTime.UtcNow);
        }

        private static bool ValidateManifest(AIDataTableManifest manifest, string jsonFile, AIDataTableReportItem item)
        {
            if (manifest.schemaVersion != SchemaVersion)
            {
                item.warnings.Add($"Schema version is {manifest.schemaVersion}, expected {SchemaVersion}.");
            }

            if (!string.Equals(manifest.kind, ManifestKind, StringComparison.Ordinal))
            {
                item.warnings.Add($"Manifest kind is '{manifest.kind}', expected '{ManifestKind}'.");
            }

            if (manifest.columns == null || manifest.columns.Count == 0)
            {
                item.errors.Add("No columns found.");
                return false;
            }

            if (manifest.rows == null)
            {
                manifest.rows = new List<AIDataTableRow>();
            }

            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var column in manifest.columns)
            {
                if (column.index <= 0)
                {
                    item.errors.Add($"Column index must be 1-based and positive. Key={column.key}");
                    continue;
                }

                if (string.IsNullOrEmpty(column.role))
                {
                    column.role = GetColumnRole(column.index, column.name, column.type);
                }

                if (string.IsNullOrEmpty(column.key))
                {
                    column.key = CreateColumnKey(column.index, column.role, column.name);
                }

                if ((column.role == RoleId || column.role == RoleData) && !keys.Add(column.key))
                {
                    item.errors.Add($"Duplicate data column key: {column.key}");
                }
            }

            var idColumn = manifest.columns.FirstOrDefault(column => column.role == RoleId);
            if (idColumn == null || idColumn.index != 2)
            {
                item.errors.Add("DataTable must keep the Id column at Excel column 2.");
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in manifest.rows)
            {
                if (row == null || !row.enabled)
                {
                    continue;
                }

                string id = GetRowFieldValue(row, idColumn);
                if (string.IsNullOrWhiteSpace(id))
                {
                    item.errors.Add($"Enabled row has empty Id. Json={jsonFile}, Row={row.row}");
                    continue;
                }

                if (!ids.Add(id))
                {
                    item.errors.Add($"Duplicate Id '{id}'. Json={jsonFile}, Row={row.row}");
                }
            }

            return item.errors.Count == 0;
        }

        private static List<string[]> BuildTableRows(AIDataTableManifest manifest, AIDataTableReportItem item, bool normalizeCustomJson)
        {
            int columnCount = GetColumnCount(manifest);
            var rows = new List<string[]>
            {
                BuildHeaderRow(manifest, columnCount, HeaderRow.TableComment),
                BuildHeaderRow(manifest, columnCount, HeaderRow.Name),
                BuildHeaderRow(manifest, columnCount, HeaderRow.Type),
                BuildHeaderRow(manifest, columnCount, HeaderRow.Comment),
            };

            foreach (var row in manifest.rows)
            {
                if (row == null)
                {
                    continue;
                }

                var cells = BuildDataRowCells(row, manifest.columns, columnCount, item, normalizeCustomJson);
                if (!IsBlankRow(cells))
                {
                    rows.Add(cells);
                }
            }

            return rows;
        }

        private static string[] BuildHeaderRow(AIDataTableManifest manifest, int columnCount, HeaderRow headerRow)
        {
            var cells = new string[columnCount];
            cells[0] = DataTableProcessor.CommentLineSeparator;

            switch (headerRow)
            {
                case HeaderRow.TableComment:
                    if (columnCount > 1)
                    {
                        cells[1] = string.IsNullOrEmpty(manifest.tableComment) ? manifest.tableName : manifest.tableComment;
                    }
                    break;
                case HeaderRow.Name:
                    FillColumnHeader(cells, manifest.columns, column => column.name);
                    cells[0] = DataTableProcessor.CommentLineSeparator;
                    break;
                case HeaderRow.Type:
                    FillColumnHeader(cells, manifest.columns, column => column.type);
                    cells[0] = DataTableProcessor.CommentLineSeparator;
                    break;
                case HeaderRow.Comment:
                    FillColumnHeader(cells, manifest.columns, column => column.comment);
                    cells[0] = DataTableProcessor.CommentLineSeparator;
                    break;
            }

            return cells;
        }

        private static void FillColumnHeader(string[] cells, IList<AIDataColumn> columns, Func<AIDataColumn, string> selector)
        {
            foreach (var column in columns)
            {
                int index = column.index - 1;
                if (index < 0 || index >= cells.Length)
                {
                    continue;
                }

                cells[index] = selector(column) ?? string.Empty;
            }
        }

        private static string[] BuildDataRowCells(AIDataTableRow row, IList<AIDataColumn> columns, int columnCount, AIDataTableReportItem item, bool normalizeCustomJson)
        {
            var cells = new string[columnCount];
            if (row.cells != null)
            {
                for (int i = 0; i < row.cells.Count && i < columnCount; i++)
                {
                    cells[i] = row.cells[i] ?? string.Empty;
                }
            }

            foreach (var column in columns)
            {
                int index = column.index - 1;
                if (index < 0 || index >= columnCount)
                {
                    continue;
                }

                if (column.role == RoleId || column.role == RoleData)
                {
                    if (row.values != null && row.values.TryGetValue(column.key, out string value))
                    {
                        cells[index] = value ?? string.Empty;
                    }
                }
                else if (column.role == RoleComment && column.index > 1)
                {
                    if (row.notes != null && row.notes.TryGetValue(column.key, out string note))
                    {
                        cells[index] = note ?? string.Empty;
                    }
                }
            }

            cells[0] = row.enabled ? string.Empty : DataTableProcessor.CommentLineSeparator;
            if (!row.enabled && !string.IsNullOrEmpty(row.comment) && columnCount > 1 && string.IsNullOrEmpty(cells[1]))
            {
                cells[1] = row.comment;
            }

            for (int i = 0; i < cells.Length; i++)
            {
                var column = columns.FirstOrDefault(value => value.index == i + 1);
                cells[i] = NormalizeCellValue(cells[i], row, column, item, normalizeCustomJson);
            }

            return cells;
        }

        private static string NormalizeCellValue(string value, AIDataTableRow row, AIDataColumn column, AIDataTableReportItem item, bool normalizeCustomJson)
        {
            value ??= string.Empty;
            if (value.Contains("\r") || value.Contains("\n"))
            {
                AddWarningOnce(item, $"Removed line break characters from row {row.row}.");
                value = Regex.Replace(value, @"[\r\n]+", string.Empty);
            }

            if (value.Contains("\t"))
            {
                AddWarningOnce(item, $"Replaced tab characters from row {row.row}; tabs are reserved as DataTable separators.");
                value = value.Replace('\t', ' ');
            }

            if (normalizeCustomJson && row.enabled && column != null && column.role == RoleData && DataTableProcessor.IsCustomJsonType(column.type))
            {
                value = DataTableProcessor.NormalizeCustomJsonValue(column.type, value);
            }

            return value;
        }

        private static bool TryValidateAndGenerateDataTable(string outputTxtFile, string relativePath, bool generateFiles, AIDataTableReportItem item)
        {
            try
            {
                var dataTableProcessor = DataTableGenerator.CreateDataTableProcessor(outputTxtFile);
                if (!DataTableGenerator.CheckRawData(dataTableProcessor, outputTxtFile))
                {
                    item.errors.Add($"Check raw data failure: {outputTxtFile}");
                    return false;
                }

                if (generateFiles)
                {
                    var appConfig = AppConfigs.GetInstanceEditor();
                    if (appConfig != null && appConfig.LoadFromBytes)
                    {
                        DataTableGenerator.GenerateDataFile(dataTableProcessor, outputTxtFile);
                    }

                    if (ShouldGenerateCode(relativePath, appConfig))
                    {
                        DataTableGenerator.GenerateCodeFile(dataTableProcessor, outputTxtFile);
                    }
                }
            }
            catch (Exception exception)
            {
                item.errors.Add(exception.ToString());
                return false;
            }

            return true;
        }

        private static bool ShouldGenerateCode(string relativePath, AppConfigs appConfig)
        {
            if (appConfig == null || appConfig.DataTables == null)
            {
                return false;
            }

            string excelFile = GameDataGenerator.GameDataExcelRelative2FullPath(GameDataType.DataTable, relativePath);
            if (GameDataGenerator.IsABTestFile(excelFile))
            {
                return false;
            }

            return appConfig.DataTables.Contains(relativePath);
        }

        private static void RefreshSpecialGeneratedScripts(HashSet<string> importedRelativePaths)
        {
            if (importedRelativePaths == null || importedRelativePaths.Count == 0)
            {
                return;
            }

            if (importedRelativePaths.Contains("Core/UITable"))
            {
                GameDataGenerator.GenerateUIFormNamesScript();
            }

            if (importedRelativePaths.Contains("Core/EntityGroupTable") ||
                importedRelativePaths.Contains("Core/SoundGroupTable") ||
                importedRelativePaths.Contains("Core/UIGroupTable"))
            {
                GameDataGenerator.GenerateGroupEnumScript();
            }
        }

        private static AIDataTableRow CreateDataRow(int rowIndex, List<string> cells, IList<AIDataColumn> columns)
        {
            var row = new AIDataTableRow
            {
                row = rowIndex,
                enabled = cells.Count == 0 || !cells[0].StartsWith(DataTableProcessor.CommentLineSeparator, StringComparison.Ordinal),
                cells = cells,
                values = new Dictionary<string, string>(StringComparer.Ordinal),
                notes = new Dictionary<string, string>(StringComparer.Ordinal),
            };

            foreach (var column in columns)
            {
                int index = column.index - 1;
                string value = index >= 0 && index < cells.Count ? cells[index] : string.Empty;
                if (column.role == RoleId || column.role == RoleData)
                {
                    row.values[column.key] = value;
                }
                else if (column.role == RoleComment && column.index > 1 && !string.IsNullOrEmpty(value))
                {
                    row.notes[column.key] = value;
                }
            }

            if (!row.enabled)
            {
                row.comment = cells.Skip(1).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
            }

            return row;
        }

        private static string GetColumnRole(int column, string name, string type)
        {
            if (column == 1)
            {
                return RoleComment;
            }

            if (column == 2)
            {
                return RoleId;
            }

            if (string.IsNullOrWhiteSpace(name) || string.Equals(name, DataTableProcessor.CommentLineSeparator, StringComparison.Ordinal) || string.Equals(type, DataTableProcessor.CommentLineSeparator, StringComparison.Ordinal))
            {
                return RoleComment;
            }

            return RoleData;
        }

        private static string CreateColumnKey(int column, string role, string name)
        {
            if (role == RoleId)
            {
                return "Id";
            }

            if (role == RoleData && !string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            return $"Note{column}";
        }

        private static string GetRowFieldValue(AIDataTableRow row, AIDataColumn column)
        {
            if (row == null || column == null)
            {
                return string.Empty;
            }

            if (row.values != null && row.values.TryGetValue(column.key, out string value))
            {
                return value ?? string.Empty;
            }

            int index = column.index - 1;
            if (row.cells != null && index >= 0 && index < row.cells.Count)
            {
                return row.cells[index] ?? string.Empty;
            }

            return string.Empty;
        }

        private static int GetColumnCount(AIDataTableManifest manifest)
        {
            int columnCount = Math.Max(2, manifest.columns.Max(column => column.index));
            if (manifest.rows != null)
            {
                foreach (var row in manifest.rows)
                {
                    if (row?.cells != null)
                    {
                        columnCount = Math.Max(columnCount, row.cells.Count);
                    }
                }
            }

            return columnCount;
        }

        private static string RowsToText(IList<string[]> rows)
        {
            var builder = new StringBuilder();
            for (int row = 0; row < rows.Count; row++)
            {
                var cells = rows[row];
                for (int column = 0; column < cells.Length; column++)
                {
                    builder.Append(cells[column] ?? string.Empty);
                    if (column < cells.Length - 1)
                    {
                        builder.Append('\t');
                    }
                }

                if (row < rows.Count - 1)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private static void WriteExcelFile(string excelFile, IList<string[]> rows)
        {
            EnsureFileDirectory(excelFile);
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var fileInfo = new FileInfo(excelFile);
            using (var excelPackage = new ExcelPackage(fileInfo))
            {
                var sheet = excelPackage.Workbook.Worksheets.Count > 0
                    ? excelPackage.Workbook.Worksheets[0]
                    : excelPackage.Workbook.Worksheets.Add("Sheet 1");

                if (sheet.Dimension != null)
                {
                    for (int row = sheet.Dimension.Start.Row; row <= sheet.Dimension.End.Row; row++)
                    {
                        for (int column = sheet.Dimension.Start.Column; column <= sheet.Dimension.End.Column; column++)
                        {
                            sheet.Cells[row, column].Value = null;
                        }
                    }
                }

                for (int row = 0; row < rows.Count; row++)
                {
                    for (int column = 0; column < rows[row].Length; column++)
                    {
                        sheet.Cells[row + 1, column + 1].Value = rows[row][column];
                    }
                }

                excelPackage.Save();
            }
        }

        private static void WriteExcelFileWithBackup(string excelFile, string relativePath, IList<string[]> rows, AIDataTableReportItem item)
        {
            var diff = CalculateExcelDiff(excelFile, rows);
            item.changedCellCount = diff.changedCellCount;
            item.changedRowCount = diff.changedRowCount;
            item.oldRowCount = diff.oldRowCount;
            item.newRowCount = diff.newRowCount;

            bool excelExisted = File.Exists(excelFile);
            string backupFile = CreateExcelBackup(excelFile, relativePath);

            try
            {
                WriteExcelFile(excelFile, rows);
                DeleteExcelBackup(backupFile);
                backupFile = null;
                GFTrace.Success("AIDataTable", "Excel.Write", null, GFTrace.Data(
                    "table", relativePath,
                    "excel", excelFile,
                    "backup", "temporary",
                    "changedCells", diff.changedCellCount.ToString(),
                    "changedRows", diff.changedRowCount.ToString()));
            }
            catch
            {
                RestoreExcelBackup(excelFile, backupFile, excelExisted);
                item.rollbackRestored = true;
                item.backupFile = backupFile;
                GFTrace.Failure("AIDataTable", "Excel.Write.Rollback", null, GFTrace.Data("table", relativePath, "excel", excelFile, "backup", backupFile));
                throw;
            }
            finally
            {
                DeleteExcelBackup(backupFile);
            }
        }

        private static AIDataTableExcelDiff CalculateExcelDiff(string excelFile, IList<string[]> rows)
        {
            var diff = new AIDataTableExcelDiff
            {
                newRowCount = rows == null ? 0 : rows.Count,
            };

            int newColumnCount = rows == null || rows.Count <= 0 ? 0 : rows.Max(row => row == null ? 0 : row.Length);
            if (!File.Exists(excelFile))
            {
                diff.changedRowCount = diff.newRowCount;
                diff.changedCellCount = rows == null ? 0 : rows.Sum(row => row == null ? 0 : row.Count(cell => !string.IsNullOrEmpty(cell)));
                return diff;
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var fileInfo = new FileInfo(excelFile);
            using (var excelPackage = new ExcelPackage(fileInfo))
            {
                var sheet = excelPackage.Workbook.Worksheets.Count > 0 ? excelPackage.Workbook.Worksheets[0] : null;
                int oldRowCount = sheet?.Dimension?.End.Row ?? 0;
                int oldColumnCount = sheet?.Dimension?.End.Column ?? 0;
                diff.oldRowCount = oldRowCount;

                int maxRowCount = Math.Max(oldRowCount, diff.newRowCount);
                int maxColumnCount = Math.Max(oldColumnCount, newColumnCount);
                for (int row = 1; row <= maxRowCount; row++)
                {
                    bool rowChanged = false;
                    for (int column = 1; column <= maxColumnCount; column++)
                    {
                        string oldValue = sheet == null || row > oldRowCount || column > oldColumnCount
                            ? string.Empty
                            : GetExcelCellString(sheet, row, column);
                        string newValue = GetRowCell(rows, row - 1, column - 1);
                        if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
                        {
                            diff.changedCellCount++;
                            rowChanged = true;
                            if (string.IsNullOrEmpty(diff.firstMismatch))
                            {
                                diff.firstMismatch = $"R{row}C{column}: excel='{TruncateForReport(oldValue)}' json='{TruncateForReport(newValue)}'";
                            }
                        }
                    }

                    if (rowChanged)
                    {
                        diff.changedRowCount++;
                    }
                }
            }

            return diff;
        }

        private static string TruncateForReport(string value)
        {
            value ??= string.Empty;
            if (value.Length <= 80)
            {
                return value;
            }

            return value.Substring(0, 77) + "...";
        }

        private static string GetRowCell(IList<string[]> rows, int row, int column)
        {
            if (rows == null || row < 0 || row >= rows.Count || rows[row] == null || column < 0 || column >= rows[row].Length)
            {
                return string.Empty;
            }

            return rows[row][column] ?? string.Empty;
        }

        private static string CreateExcelBackup(string excelFile, string relativePath)
        {
            if (!File.Exists(excelFile))
            {
                return null;
            }

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string backupFile = UtilityBuiltin.AssetsPath.GetCombinePath(
                projectRoot,
                "Temp/AIDataExcelBackups/DataTables",
                $"{ToRelativePathWithoutExtension(relativePath)}.{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            EnsureFileDirectory(backupFile);
            File.Copy(excelFile, backupFile, true);
            return backupFile;
        }

        private static void RestoreExcelBackup(string excelFile, string backupFile, bool excelExisted)
        {
            if (excelExisted && !string.IsNullOrWhiteSpace(backupFile) && File.Exists(backupFile))
            {
                EnsureFileDirectory(excelFile);
                File.Copy(backupFile, excelFile, true);
            }
            else if (!excelExisted && File.Exists(excelFile))
            {
                File.Delete(excelFile);
            }
        }

        private static void DeleteExcelBackup(string backupFile)
        {
            if (!string.IsNullOrWhiteSpace(backupFile) && File.Exists(backupFile))
            {
                File.Delete(backupFile);
            }
        }

        private static IList<string> GetAllAIJsonFiles()
        {
            if (!Directory.Exists(ConstEditor.AIDataTablePath))
            {
                return Array.Empty<string>();
            }

            return Directory.GetFiles(ConstEditor.AIDataTablePath, "*.json", SearchOption.AllDirectories)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static bool IsBusinessAIJsonFile(string jsonFile)
        {
            if (string.IsNullOrWhiteSpace(jsonFile))
            {
                return false;
            }

            string relativeJson = Path.GetRelativePath(ConstEditor.AIDataTablePath, jsonFile);
            string relativePath = ToRelativePathWithoutExtension(relativeJson).Replace('\\', '/');
            return relativePath.StartsWith("Business/", StringComparison.OrdinalIgnoreCase);
        }

        private static IList<string> GetAIJsonFilesByRelativePaths(IList<string> relativePaths, out List<string> missingFiles)
        {
            var result = new List<string>();
            missingFiles = new List<string>();
            if (relativePaths == null)
            {
                return result;
            }

            foreach (var relativePath in relativePaths)
            {
                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    continue;
                }

                string jsonFile = GetAIJsonFile(relativePath);
                if (File.Exists(jsonFile))
                {
                    result.Add(jsonFile);
                }
                else
                {
                    missingFiles.Add(jsonFile);
                }
            }

            return result;
        }

        private static string GetAIJsonFile(string relativePath)
        {
            return UtilityBuiltin.AssetsPath.GetCombinePath(ConstEditor.AIDataTablePath, relativePath + ".json");
        }

        private static string GetManifestRelativePath(AIDataTableManifest manifest, string jsonFile)
        {
            if (!string.IsNullOrWhiteSpace(manifest.relativePath))
            {
                return ToRelativePathWithoutExtension(manifest.relativePath);
            }

            string relativeJson = Path.GetRelativePath(ConstEditor.AIDataTablePath, jsonFile);
            return ToRelativePathWithoutExtension(relativeJson);
        }

        private static string ToRelativePathWithoutExtension(string path)
        {
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            return string.IsNullOrEmpty(directory) ? fileName : UtilityBuiltin.AssetsPath.GetCombinePath(directory, fileName);
        }

        private static string GetExcelCellString(ExcelWorksheet sheet, int row, int column)
        {
            string value = sheet.GetValue(row, column)?.ToString() ?? string.Empty;
            return Regex.Replace(value, @"[\r\n]+", string.Empty);
        }

        private static string GetTempExcelFile(string excelFile)
        {
            string directory = Path.GetDirectoryName(excelFile);
            string fileName = $"{Path.GetFileName(excelFile)}.{Guid.NewGuid():N}.ai.tmp";
            return UtilityBuiltin.AssetsPath.GetCombinePath(directory, fileName);
        }

        private static string GetValidateTempPath()
        {
            return UtilityBuiltin.AssetsPath.GetCombinePath(Directory.GetParent(Application.dataPath).FullName, "Temp/AIDataTableValidate");
        }

        private static string ToProjectRelativePath(string fullPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.GetRelativePath(projectRoot, fullPath).Replace('\\', '/');
        }

        private static bool IsBlankRow(IList<string> cells)
        {
            return cells == null || cells.All(string.IsNullOrWhiteSpace);
        }

        private static void EnsureFileDirectory(string fileName)
        {
            string directoryName = Path.GetDirectoryName(fileName);
            if (!string.IsNullOrWhiteSpace(directoryName) && !Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
        }

        private static void WriteReport(AIDataTableReport report)
        {
            EnsureDirectory(ConstEditor.AIDataReportPath);
            string reportFile = UtilityBuiltin.AssetsPath.GetCombinePath(ConstEditor.AIDataReportPath, $"{report.action}_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.WriteAllText(reportFile, JsonConvert.SerializeObject(report, Formatting.Indented), Utf8NoBom);
            Debug.Log($"AI DataTable report: {reportFile}");
        }

        private static void EnsureDirectory(string directoryName)
        {
            if (!string.IsNullOrWhiteSpace(directoryName) && !Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
        }

        private static void AddWarningOnce(AIDataTableReportItem item, string message)
        {
            if (!item.warnings.Contains(message))
            {
                item.warnings.Add(message);
            }
        }

        private enum HeaderRow
        {
            TableComment,
            Name,
            Type,
            Comment,
        }

        private sealed class AIDataTableExcelDiff
        {
            public int changedCellCount;
            public int changedRowCount;
            public int oldRowCount;
            public int newRowCount;
            public string firstMismatch;
        }
    }

    [Serializable]
    public sealed class AIDataTableManifest
    {
        public int schemaVersion;
        public string kind;
        public string tableName;
        public string relativePath;
        public string sourceExcel;
        public string sourceExcelLastWriteUtc;
        public string generatedAtUtc;
        public string tableComment;
        public List<AIDataColumn> columns = new List<AIDataColumn>();
        public List<AIDataTableRow> rows = new List<AIDataTableRow>();
    }

    [Serializable]
    public sealed class AIDataColumn
    {
        public int index;
        public string key;
        public string name;
        public string type;
        public string comment;
        public string role;
    }

    [Serializable]
    public sealed class AIDataTableRow
    {
        public int row;
        public bool enabled = true;
        public string comment;
        public Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);
        public Dictionary<string, string> notes = new Dictionary<string, string>(StringComparer.Ordinal);

        [JsonProperty("_cells")]
        public List<string> cells = new List<string>();
    }

    [Serializable]
    public sealed class AIDataTableReport
    {
        public string action;
        public string createdAtUtc;
        public int successCount;
        public int failureCount;
        public int warningCount;
        public List<string> warnings = new List<string>();
        public List<AIDataTableReportItem> items = new List<AIDataTableReportItem>();

        public AIDataTableReport(string action)
        {
            this.action = action;
            createdAtUtc = DateTime.UtcNow.ToString("O");
        }

        public AIDataTableReportItem AddItem(string sourceFile, string jsonFile)
        {
            var item = new AIDataTableReportItem
            {
                sourceFile = sourceFile,
                jsonFile = jsonFile,
            };
            items.Add(item);
            return item;
        }

        public void RefreshSummary()
        {
            successCount = items.Count(item => item.success);
            failureCount = items.Count(item => !item.success);
            warningCount = warnings.Count + items.Sum(item => item.warnings.Count);
        }
    }

    [Serializable]
    public sealed class AIDataTableReportItem
    {
        public bool success;
        public string tableName;
        public string sourceFile;
        public string jsonFile;
        public string outputFile;
        public string backupFile;
        public int changedCellCount;
        public int changedRowCount;
        public int oldRowCount;
        public int newRowCount;
        public bool rollbackRestored;
        public List<string> warnings = new List<string>();
        public List<string> errors = new List<string>();
    }
}
#endif
