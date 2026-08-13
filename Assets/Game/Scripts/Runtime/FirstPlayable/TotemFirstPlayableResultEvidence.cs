using System;
using System.IO;
using System.Text;
using UnityEngine;

[Serializable]
public sealed class TotemFirstPlayableResultEvidence
{
    public int schemaVersion = 1;
    public string configVersion = TotemFirstPlayableRules.ConfigVersion;
    public string capturedUtc = string.Empty;
    public int seed;
    public bool fastMode;
    public string finalPhase = string.Empty;
    public float totalElapsedSeconds;
    public string resultReason = string.Empty;
    public bool localTeamWon;
    public bool localTeamExtracted;
    public bool draw;
    public int winnerTeamId = -1;
    public int[] buildSeconds = Array.Empty<int>();
    public int combatSeconds;
    public int shrinkSeconds;
    public TotemResultTeamEvidence[] teams = Array.Empty<TotemResultTeamEvidence>();
    public TotemResultParticipantEvidence[] participants = Array.Empty<TotemResultParticipantEvidence>();
    public string[] anomalies = Array.Empty<string>();
    public string[] keyConfigs = Array.Empty<string>();
}

[Serializable]
public sealed class TotemResultTeamEvidence
{
    public int teamId;
    public int aliveCount;
    public float remainingHealth;
}

[Serializable]
public sealed class TotemResultParticipantEvidence
{
    public int participantId;
    public int teamId;
    public string controller = string.Empty;
    public string lifecycle = string.Empty;
    public bool alive;
    public float health;
    public float maxHealth;
    public TotemPublicTattooSnapshotEntry[] tattoos = Array.Empty<TotemPublicTattooSnapshotEntry>();
    public TotemMatchAchievementSnapshot achievements;
}

public static class TotemFirstPlayableResultEvidenceWriter
{
    public static string GetDefaultDirectory() =>
        Path.Combine(Application.persistentDataPath, "FirstPlayableEvidence");

    public static TotemFirstPlayableResultEvidence Build(
        TotemGameRuntime runtime,
        TotemUIService ui,
        TotemRunResultSnapshot result)
    {
        bool fastMode = ui?.LastLocalMatchFastMode ?? false;
        var evidence = new TotemFirstPlayableResultEvidence
        {
            capturedUtc = DateTime.UtcNow.ToString("O"),
            seed = ui?.LastLocalMatchSeed ?? 1,
            fastMode = fastMode,
            finalPhase = runtime?.GetService<TotemMatchFlowService>()?.CurrentPhase.ToString() ?? TotemMatchPhase.Result.ToString(),
            totalElapsedSeconds = Mathf.Max(0f, result?.elapsedSec ?? 0f),
            resultReason = result?.reason ?? string.Empty,
            localTeamWon = result?.win ?? false,
            localTeamExtracted = result?.extracted ?? false,
            draw = result?.draw ?? false,
            winnerTeamId = result?.winnerTeamId ?? -1,
            buildSeconds = new[]
            {
                TotemFirstPlayableRules.OpeningBuildSeconds,
                TotemFirstPlayableRules.LaterBuildSeconds,
                TotemFirstPlayableRules.LaterBuildSeconds,
                TotemFirstPlayableRules.LaterBuildSeconds,
                TotemFirstPlayableRules.LaterBuildSeconds,
            },
            combatSeconds = fastMode ? TotemFirstPlayableRules.FastCombatSeconds : TotemFirstPlayableRules.NormalCombatSeconds,
            shrinkSeconds = fastMode ? TotemFirstPlayableRules.FastShrinkSeconds : TotemFirstPlayableRules.NormalShrinkSeconds,
            keyConfigs = new[]
            {
                TotemFirstPlayableRules.ConfigVersion,
                "MapResourcePickupConfig:9",
                "ZoneShrinkConfig:4",
                "Weapon:" + TotemWeaponService.DefaultWeaponId,
            },
        };

        TotemActorService actorService = runtime?.GetService<TotemActorService>();
        TotemFirstPlayableTattooBuildService buildService = runtime?.GetService<TotemFirstPlayableTattooBuildService>();
        TotemFirstPlayableSocialService socialService = runtime?.GetService<TotemFirstPlayableSocialService>();
        int actorCount = actorService?.Actors?.Count ?? 0;
        evidence.participants = new TotemResultParticipantEvidence[actorCount];
        evidence.teams = new TotemResultTeamEvidence[TotemFirstPlayableRules.TeamCount];
        for (int teamId = 0; teamId < evidence.teams.Length; teamId++)
        {
            evidence.teams[teamId] = new TotemResultTeamEvidence { teamId = teamId };
        }

        for (int i = 0; i < actorCount; i++)
        {
            TotemActorModel actor = actorService.Actors[i];
            TotemFirstPlayableTattooBuildState build = buildService?.GetOrCreateState(actor);
            TotemConstructionIntelligenceSnapshot snapshot = TotemFirstPlayableSocialService.CreateBoundarySnapshot(
                actor,
                build,
                socialService?.CaptureAchievement(new TotemParticipantId(actor.ParticipantId)) ?? default,
                TotemMatchPhase.Result);
            evidence.participants[i] = new TotemResultParticipantEvidence
            {
                participantId = actor.ParticipantId,
                teamId = actor.TeamId.Value,
                controller = actor.ControllerKind.ToString(),
                lifecycle = actor.Lifecycle.ToString(),
                alive = actor.IsAlive,
                health = Mathf.Max(0f, actor.Health),
                maxHealth = Mathf.Max(0f, actor.MaxHealth),
                tattoos = snapshot?.tattoos ?? Array.Empty<TotemPublicTattooSnapshotEntry>(),
                achievements = snapshot?.achievements ?? default,
            };

            if (actor.TeamId.IsValid)
            {
                TotemResultTeamEvidence team = evidence.teams[actor.TeamId.Value];
                if (actor.IsAlive)
                {
                    team.aliveCount++;
                }

                team.remainingHealth += Mathf.Max(0f, actor.Health);
            }
        }

        TotemParticipantReadinessSnapshot readiness = runtime?.GetService<TotemParticipantReadinessService>()?.CaptureSnapshot();
        if (readiness != null && readiness.timeoutCount > 0)
        {
            evidence.anomalies = new[] { "ParticipantReadinessTimeout:" + readiness.timeoutCount };
        }

        return evidence;
    }

    public static bool TryWrite(
        string directory,
        TotemFirstPlayableResultEvidence evidence,
        out string outputFile,
        out string error)
    {
        outputFile = string.Empty;
        error = string.Empty;
        if (evidence == null)
        {
            error = "Evidence is null.";
            return false;
        }

        try
        {
            string targetDirectory = string.IsNullOrWhiteSpace(directory) ? GetDefaultDirectory() : directory;
            Directory.CreateDirectory(targetDirectory);
            string json = JsonUtility.ToJson(evidence, true);
            string stableFile = Path.Combine(targetDirectory, "latest.json");
            string replayFile = Path.Combine(targetDirectory, $"seed-{evidence.seed}-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.json");
            WriteAtomic(stableFile, json);
            WriteAtomic(replayFile, json);
            outputFile = replayFile;
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }

    private static void WriteAtomic(string fileName, string content)
    {
        string temporary = fileName + ".tmp";
        File.WriteAllText(temporary, content, new UTF8Encoding(false));
        if (File.Exists(fileName))
        {
            File.Delete(fileName);
        }

        File.Move(temporary, fileName);
    }
}
