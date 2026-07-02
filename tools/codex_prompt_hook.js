#!/usr/bin/env node
/*
 * Codex hook helper.
 *
 * Keep Codex prompt injection close to the Claude workflow without modifying
 * .claude/*. Claude remains the source workflow; this file only adapts those
 * reminders to Codex's hook/runtime surface.
 */

const fs = require("fs");

function readStdin() {
  return new Promise((resolve) => {
    let data = "";
    process.stdin.setEncoding("utf8");
    process.stdin.on("data", (chunk) => {
      data += chunk;
    });
    process.stdin.on("end", () => resolve(data));
    process.stdin.on("error", () => resolve(""));
  });
}

function emit(eventName, additionalContext) {
  if (!additionalContext) {
    return;
  }

  console.log(JSON.stringify({
    hookSpecificOutput: {
      hookEventName: eventName,
      additionalContext,
    },
  }));
}

function baseContext() {
  return [
    "你是一个精通 C# 和 Unity 开发的游戏开发工程师，具备以下特质：",
    "- 注重代码的灵活性、稳定性、可扩展性和可读性。",
    "- 以实用性为主，在能够优秀实现需求的前提下，不做过度扩展。优先用简单方案，不要过度工程。",
    "- 编写代码时非常注意时序问题。",
    "- 擅长性能优化，提供高性能的解决方案。",
    "- 能够独立思考和批判性思考问题，当用户的需求有问题时，会主动指出并提供建议。",
    "- 始终使用中文回答。",
    "- 所有按键输入必须走 InputModule。",
    "- Claude 工作流是语义源；Codex 执行时按 AGENTS.md 的 Codex 适配规则等价落地。",
  ].join("\n");
}

function sessionContext() {
  try {
    const agents = fs.readFileSync("AGENTS.md", "utf8");
    return `【自动注入 AGENTS.md - Codex 入口与 Claude 等价工作流】\n\n${agents}`;
  } catch (_err) {
    return "";
  }
}

function parsePrompt(raw) {
  try {
    const payload = JSON.parse(raw || "{}");
    return payload.prompt || "";
  } catch (_err) {
    return "";
  }
}

function decisionGateContext(prompt) {
  const re = /设计|架构|重构|大改|重写|GDD|PRD|系统|范式|方案|思路/;
  if (!re.test(prompt)) {
    return "";
  }

  return [
    "检测到「大型决策」关键词。按 Claude 工作流的两阶段 FSM 执行：",
    "阶段 A：先用 grill-me / grill-with-docs 的问题框架多轮反问，直到 5 条全清才能退出：1)目标 2)关键决策(A/B 比较) 3)边界 4)验收标准 5)约束。",
    "阶段 B：挖透后先做任务规模评估；命中 openspec 信号则创建 openspec change，否则走轻量路径。执行期间模糊点按阶段 A 共识自决。",
    "仅以下情况可中断阶段 B：与阶段 A 共识冲突 / 不可逆变更 / 改动 .claude、openspec、Assets/Scripts/Core 框架核心。",
    "Codex 适配：agent 路由先按 .codex/agents/*.toml 读取对应职责和白名单；若当前 Codex 运行时不能原生调用该 agent，则主对话按该 agent prompt 与 skill 白名单等价执行。",
  ].join("\n");
}

function artInitContext(prompt) {
  if (!/初始化美术|设计美术风格|初始化.*美术/.test(prompt)) {
    return "";
  }

  return "检测到用户想要初始化美术风格。根据项目规范，应按 ai-art SKILL 的美术风格初始化流程沉淀需求，而不是手动凭空编写文档。";
}

function imageGenContext(prompt) {
  const re = /出图|生图|生成图片|绘制美术|绘制素材|实现.*美术素材|处理.*美术素材|按提示词出图|把美术需求出图|按需求出图|生成美术素材/;
  if (!re.test(prompt)) {
    return "";
  }

  return [
    "检测到用户希望生成美术素材。执行规则：",
    "1) 前置：openspec/changes/<change>/art/requirements.md 与 prompts.md 必须已存在且完整；若缺失，先按 ai-art SKILL 的「美术素材实现流程」补齐。",
    "2) 出图阶段必须调用 codex-image-gen / codex-art-gen 对应能力，实际生成 PNG 到 art/raw/ 或 art/mockups/。",
    "3) 严禁伪造文件、严禁假装已生成；失败逐项记录到同目录生成记录.md。",
  ].join("\n");
}

function graphifyContext(prompt) {
  if (!prompt.trim().startsWith("/graphify")) {
    return "";
  }

  return "检测到 /graphify。Codex 中应优先使用 graphify-windows skill，并在做其他操作前读取该 skill 的 SKILL.md。";
}

async function main() {
  const mode = process.argv[2] || "prompt";

  if (mode === "session") {
    emit("SessionStart", sessionContext());
    return;
  }

  const raw = await readStdin();
  const prompt = parsePrompt(raw);
  const contexts = [
    baseContext(),
    graphifyContext(prompt),
    decisionGateContext(prompt),
    artInitContext(prompt),
    imageGenContext(prompt),
  ].filter(Boolean);

  emit("UserPromptSubmit", contexts.join("\n\n"));
}

main().catch(() => {
  // Hooks must never block the actual task.
});
