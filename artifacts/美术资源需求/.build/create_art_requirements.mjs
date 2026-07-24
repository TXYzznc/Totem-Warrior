import fs from "node:fs/promises";
import path from "node:path";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const root = path.resolve(import.meta.dirname, "..");
const outputPath = path.join(root, "美术资源需求表.xlsx");

const assets = [
  [1, "CHR-001 基础角色模型与统一骨骼", "模型", "模型/CHR-001_基础角色模型与统一骨骼/", "中性基础身体；局部功能性机械改造；统一骨骼；六处固定纹身 UV 画布。已验收：A/B/C 三套设计稿及每套正/侧/背独立画布。", "已完成-符合"],
  [3, "CHR-003 轻机能服装与局部机械组件", "模型", "模型/CHR-003_轻机能服装与局部机械组件/", "短夹克、背带、护腕、护膝、分段裤等；不得遮挡六处刺青画布。已验收：A/B/C 三套设计稿及每套正/侧/背独立画布。", "已完成-符合"],
  [4, "WPN-001 基础武器组", "模型", "模型/WPN-001_基础武器组/", "短刀、重锤、手枪、弓、能量拳套；统一明亮活性颜料世界的轮廓与材质。", "待分配"],
  [5, "NPC-001 活性颜料污染 NPC", "模型", "模型/NPC-001_活性颜料污染NPC/", "明亮却危险的污染生物/失控维护体；需支持 PVE 识别与攻击前摇。", "待分配"],
  [6, "ENV-001 街区建筑外立面模块", "模型", "模型/ENV-001_街区建筑外立面模块/", "未来生活街区的墙、窗、门、屋顶、阳台、招牌与立面组件。", "待分配"],
  [7, "ENV-002 可进入建筑室内模块", "模型", "模型/ENV-002_可进入建筑室内模块/", "楼板、隔断、走廊、楼梯、房门、室内窗与转移路线组件。", "待分配"],
  [8, "ENV-003 生活化街区道具组", "模型", "模型/ENV-003_生活化街区道具组/", "便利店、诊所、纹身店、咖啡馆、公寓、快递柜、地铁口等生活场景道具。", "待分配"],
  [9, "ENV-004 资源与交互物件组", "模型", "模型/ENV-004_资源与交互物件组/", "补给点、死亡箱、颜料容器、样式模板容器、撤离地标和事件物件。", "待分配"],
  [10, "ENV-005 活性颜料污染结构组", "模型", "模型/ENV-005_活性颜料污染结构组/", "颜料蔓延、变异植物/结晶、泄漏管线和 NPC 巢穴；用于让生活街区显得局部失控。", "待分配"],
  [11, "UI-001 常驻 HUD 组件", "UI", "UI/UI-001_常驻HUD组件/", "生存、武器、颜料、六部位构筑摘要、事件与撤离提示；PC 键鼠信息层级。已验收：A/B/C 三套全状态视觉稿。", "已完成-符合"],
  [12, "UI-002 纹身工作台界面", "UI", "UI/UI-002_纹身工作台界面/", "角色预览、六部位、颜料库存、样式库存、一次性次数、效果对比和读条/取消反馈。已验收：A/B/C 三套主界面视觉稿。", "已完成-符合"],
  [13, "UI-003 七色元素与八图案图标", "UI", "UI/UI-003_元素与图案图标/", "七种颜料元素与八种图案的完整图标套件；需同时服务 HUD、工作台、掉落物和图鉴。", "待分配"],
  [14, "UI-004 物资、死亡箱与撤离反馈", "UI", "UI/UI-004_物资死亡箱撤离反馈/", "物资稀有度、可掉落标记、死亡箱、撤离读条、晚期事件和高价值补给反馈。", "待分配"],
  [15, "UI-005 局外熟练度与构筑档案", "UI", "UI/UI-005_熟练度与构筑档案/", "八图案熟练度、样式权限、构筑快照、战绩与外观展示界面。", "待分配"],
  [16, "GEN-001 六部位纹身贴花与遮罩", "通用", "通用/GEN-001_纹身贴花与遮罩/", "六部位贴花模板、八图案基础设计、颜色变体与动态映射遮罩。", "待分配"],
  [17, "GEN-002 活性颜料材质与 Shader", "通用", "通用/GEN-002_活性颜料材质与Shader/", "角色合成真皮、机械接口、建筑污染、能量流和局部发光的统一材质规则。", "待分配"],
  [18, "GEN-003 纹身与元素 VFX", "通用", "通用/GEN-003_纹身与元素VFX/", "刺青触发、元素命中、局部发光、读条、覆盖和获得样式的共享特效。", "待分配"],
  [19, "GEN-004 灯光、天空与后处理", "通用", "通用/GEN-004_灯光天空后处理/", "晴天、明亮生活街区、局部高饱和污染和终局收束的全局灯光/后处理基线。", "待分配"],
  [20, "GEN-005 模块化贴图与 Trim Sheet", "通用", "通用/GEN-005_模块化贴图与TrimSheet/", "街区建筑、室内、生活道具和局部机械件可共享的贴图、贴花和 Trim Sheet。", "待分配"],
];

// 编号仅用于台账排序；移除需求后始终保持从 1 连续递增。
assets.forEach((asset, index) => { asset[0] = index + 1; });

const statuses = ["待分配", "待寻找", "待制作", "待验收", "已完成-符合", "已完成-需返工", "暂不需要"];

const workbook = Workbook.create();
const sheet = workbook.worksheets.add("资源需求表");
const statusSheet = workbook.worksheets.add("状态说明");
sheet.showGridLines = false;
statusSheet.showGridLines = false;

sheet.mergeCells("A1:F1");
sheet.getRange("A1").values = [["美术资源需求总表 | 3D PvPvE 美术垂直切片与后续生产"]];
sheet.getRange("A1:F1").format = {
  fill: "#1F4D78",
  font: { bold: true, color: "#FFFFFF", size: 16 },
  horizontalAlignment: "center",
  verticalAlignment: "center",
};
sheet.getRange("A1:F1").format.rowHeight = 30;
sheet.getRange("A2:F2").merge();
sheet.getRange("A2").values = [["维护规则：先更新台账，再按“路径”放入资源；验收通过后更新状态。路径均相对本文件所在目录。"]];
sheet.getRange("A2:F2").format = {
  fill: "#E8EEF5",
  font: { color: "#1F4D78", italic: true, size: 10 },
  horizontalAlignment: "left",
  verticalAlignment: "center",
  wrapText: true,
};
sheet.getRange("A2:F2").format.rowHeight = 28;

const headers = [["编号", "名称", "类型", "路径", "描述", "状态"]];
sheet.getRange("A3:F3").values = headers;
sheet.getRange(`A4:F${assets.length + 3}`).values = assets;
sheet.getRange(`A3:F${assets.length + 3}`).format = {
  borders: { preset: "inside", style: "thin", color: "#D9E1EA" },
  verticalAlignment: "center",
};
sheet.getRange("A3:F3").format = {
  fill: "#2E74B5",
  font: { bold: true, color: "#FFFFFF" },
  horizontalAlignment: "center",
  verticalAlignment: "center",
};
sheet.getRange(`A4:A${assets.length + 3}`).format.horizontalAlignment = "center";
sheet.getRange(`C4:C${assets.length + 3}`).format.horizontalAlignment = "center";
sheet.getRange(`F4:F${assets.length + 3}`).format.horizontalAlignment = "center";
sheet.getRange(`D4:E${assets.length + 3}`).format.wrapText = true;
sheet.getRange(`A4:F${assets.length + 3}`).format.rowHeight = 40;
sheet.getRange("A:A").format.columnWidth = 9;
sheet.getRange("B:B").format.columnWidth = 33;
sheet.getRange("C:C").format.columnWidth = 12;
sheet.getRange("D:D").format.columnWidth = 42;
sheet.getRange("E:E").format.columnWidth = 70;
sheet.getRange("F:F").format.columnWidth = 18;
sheet.freezePanes.freezeRows(3);
sheet.freezePanes.freezeColumns(1);
sheet.tables.add(`A3:F${assets.length + 3}`, true, "ArtRequirementsTable");
sheet.getRange("F4:F200").dataValidation = { rule: { type: "list", values: statuses } };
sheet.getRange("F4:F200").conditionalFormats.add("containsText", { text: "待", format: { fill: "#FFF2CC", font: { color: "#7A5A00" } } });
sheet.getRange("F4:F200").conditionalFormats.add("containsText", { text: "已完成-符合", format: { fill: "#E2F0D9", font: { color: "#375623", bold: true } } });
sheet.getRange("F4:F200").conditionalFormats.add("containsText", { text: "需返工", format: { fill: "#FCE4D6", font: { color: "#9B1C1C", bold: true } } });

statusSheet.getRange("A1:B1").merge();
statusSheet.getRange("A1").values = [["资源状态说明"]];
statusSheet.getRange("A1:B1").format = { fill: "#1F4D78", font: { bold: true, color: "#FFFFFF", size: 15 }, horizontalAlignment: "center" };
statusSheet.getRange("A3:B3").values = [["状态", "使用条件"]];
statusSheet.getRange("A4:B10").values = [
  ["待分配", "尚未决定由用户寻找、采购还是由 Codex 制作。"],
  ["待寻找", "用户正在寻找或采购候选资源；未验收。"],
  ["待制作", "已明确由 Codex 或项目成员制作；未验收。"],
  ["待验收", "资源已放入规定路径，等待风格、格式和需求验收。"],
  ["已完成-符合", "已验收通过，可用于当前范围。"],
  ["已完成-需返工", "资源已存在但未满足需求，保留原路径等待返工。"],
  ["暂不需要", "已从当前范围移出，保留记录避免重复采购或制作。"],
];
statusSheet.getRange("A3:B10").format = { borders: { preset: "inside", style: "thin", color: "#D9E1EA" }, verticalAlignment: "center" };
statusSheet.getRange("A3:B3").format = { fill: "#2E74B5", font: { bold: true, color: "#FFFFFF" }, horizontalAlignment: "center" };
statusSheet.getRange("A4:B10").format.rowHeight = 34;
statusSheet.getRange("B4:B10").format.wrapText = true;
statusSheet.getRange("A:A").format.columnWidth = 20;
statusSheet.getRange("B:B").format.columnWidth = 75;
statusSheet.freezePanes.freezeRows(3);

await fs.mkdir(root, { recursive: true });
const tableRange = `A1:F${assets.length + 3}`;
const inspection = await workbook.inspect({ kind: "table", range: `资源需求表!${tableRange}`, include: "values,formulas", tableMaxRows: 24, tableMaxCols: 6 });
console.log(inspection.ndjson);
const errors = await workbook.inspect({ kind: "match", searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A", options: { useRegex: true, maxResults: 50 }, summary: "formula error scan" });
console.log(errors.ndjson);
const preview = await workbook.render({ sheetName: "资源需求表", range: tableRange, scale: 1.5, format: "png" });
await fs.writeFile(path.join(root, ".build", "资源需求表预览.png"), new Uint8Array(await preview.arrayBuffer()));
const statusPreview = await workbook.render({ sheetName: "状态说明", range: "A1:B10", scale: 1.5, format: "png" });
await fs.writeFile(path.join(root, ".build", "状态说明预览.png"), new Uint8Array(await statusPreview.arrayBuffer()));
const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(outputPath);
console.log(outputPath);
