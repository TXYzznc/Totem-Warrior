using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace PCGMap
{
    [Serializable]
    public sealed class PCGEdgeDefinition
    {
        public string north;
        public string east;
        public string south;
        public string west;
    }

    [Serializable]
    public sealed class PCGSizeDefinition
    {
        public int width = 1;
        public int height = 1;
    }

    [Serializable]
    public sealed class PCGPlacementDefinition
    {
        public string[] allowedZones;
        public string[] forbiddenZones;
        public string[] requiresTerrain;
        public string[] forbidTerrain;
        public int minDistanceToRoad;
        public int minDistanceToWater;
        public int minDistanceToSpawn;
        public bool requiresEmptyArea;
        public int requiredEscapeGaps;
    }

    [Serializable]
    public sealed class TerrainVisualEntry
    {
        public string id;
        public string asset;
        public string biome;
        public string terrain;
        public string useCase;
        public PCGEdgeDefinition edge;
        public string[] allowedNeighbors;
        public string[] forbiddenNeighbors;
        public PCGSizeDefinition size;
        public PCGPlacementDefinition placement;
        public int weight = 1;
        public string rarity;
        public string[] tags;
    }

    [Serializable]
    public sealed class TerrainPriorityEntry
    {
        public string terrain;
        public int priority;
    }

    [Serializable]
    public sealed class TerrainTransitionMaskEntry
    {
        public string id;
        public string asset;
        public string maskSet;
        public string direction;
        public float rotationDegrees;
        public int sortingOffset = 40;
        public int weight = 1;
        public string[] tags;
    }

    [Serializable]
    public sealed class TerrainTransitionDetailEntry
    {
        public string id;
        public string asset;
        public string detailSet;
        public int sortingOffset = 50;
        public float chance = 1f;
        public int weight = 1;
        public string[] tags;
    }

    [Serializable]
    public sealed class TerrainTransitionRuleEntry
    {
        public string from;
        public string to;
        public string maskSet;
        public string[] detailSets;
        public float detailChance = 0.35f;
    }

    [Serializable]
    public sealed class TerrainVisualCatalog
    {
        public int schemaVersion;
        public string catalogId;
        public string assetPathMode;
        public int pixelsPerUnit = 128;
        public List<TerrainVisualEntry> tiles = new();
        public List<TerrainPriorityEntry> terrainPriorities = new();
        public List<TerrainTransitionMaskEntry> transitionMasks = new();
        public List<TerrainTransitionDetailEntry> transitionDetails = new();
        public List<TerrainTransitionRuleEntry> transitionRules = new();
    }

    [Serializable]
    public sealed class TerrainTileSourceDefinition
    {
        public string sheet;
        public int x;
        public int y;
    }

    [Serializable]
    public sealed class TerrainTileEdgeSocket
    {
        public string localTerrain;
        public string neighborTerrain;
        public string profile;
        public string signature;
    }

    [Serializable]
    public sealed class TerrainTileEdges
    {
        public TerrainTileEdgeSocket north;
        public TerrainTileEdgeSocket east;
        public TerrainTileEdgeSocket south;
        public TerrainTileEdgeSocket west;
    }

    [Serializable]
    public sealed class TerrainTileOriginalNeighbors
    {
        public string north;
        public string east;
        public string south;
        public string west;
    }

    [Serializable]
    public sealed class TerrainEdgeSignatureEntry
    {
        public int stripWidth;
        public int sampleCount;
        public Dictionary<string, float> terrainRatio;
        public int[][] colorSamples;
        public float lumaMean;
        public float lumaVariance;
        public float[] edgeShape;
    }

    [Serializable]
    public sealed class TerrainTileEntry
    {
        public string id;
        public string asset;
        public TerrainTileSourceDefinition source;
        public string centerTerrain;
        public Dictionary<string, float> terrainRatio;
        public TerrainTileEdges edges;
        public List<TerrainTileOrientationEntry> orientations;
        public TerrainTileOriginalNeighbors originalNeighbors;
        public int weight = 1;
        public string[] tags;
    }

    [Serializable]
    public sealed class TerrainTileOrientationEntry
    {
        public string id;
        public string asset;
        public int rotationSteps;
        public float rotationDegrees;
        public bool flipX;
        public TerrainTileEdges edges;
    }

    [Serializable]
    public sealed class TerrainTileSetCatalog
    {
        public int schemaVersion;
        public string catalogId;
        public string assetPathMode;
        public int cellSizePixels = 128;
        public int gridSize = 16;
        public Dictionary<string, object> edgeSignature;
        public List<TerrainTileEntry> tiles = new();
        public Dictionary<string, TerrainEdgeSignatureEntry> signatures = new();
    }

    public sealed class TerrainTilePickResult
    {
        public TerrainTileEntry Tile;
        public string Asset;
        public TerrainTileEdges Edges;
        public int RotationSteps;
        public float RotationDegrees;
        public bool FlipX;
    }

    [Serializable]
    public sealed class WorldObjectEntry
    {
        public string id;
        public string asset;
        public string objectRole;
        public string combatRole;
        public string[] allowedBiomes;
        public string[] allowedTerrains;
        public PCGSizeDefinition footprint;
        public bool blocksMovement;
        public bool blocksSight;
        public int minSpacing;
        public float density = 1f;
        public int placementPriority = 50;
        public PCGPlacementDefinition placement;
        public int weight = 1;
        public string[] tags;
    }

    [Serializable]
    public sealed class PoiEntry
    {
        public string id;
        public string asset;
        public string zoneRole;
        public PCGSizeDefinition footprint;
        public string lootRole;
        public string coverRole;
        public string monsterRole;
        public PCGPlacementDefinition placement;
        public int weight = 1;
        public string[] tags;
    }

    [Serializable]
    public sealed class WorldObjectCatalog
    {
        public int schemaVersion;
        public string catalogId;
        public string assetPathMode;
        public int pixelsPerUnit = 128;
        public List<WorldObjectEntry> objects = new();
        public List<PoiEntry> pois = new();
    }

    [Serializable]
    public sealed class ZoneRuleEntry
    {
        public string id;
        public string displayName;
        public Dictionary<string, float> terrainBias;
        public float objectDensityMultiplier = 1f;
        public float lootDensityMultiplier = 1f;
        public float danger;
        public int minRadius;
    }

    [Serializable]
    public sealed class ZoneRuleCatalog
    {
        public int schemaVersion;
        public string catalogId;
        public List<ZoneRuleEntry> zones = new();
    }

    public sealed class PCGAssetIndex
    {
        readonly Dictionary<string, TerrainVisualEntry> _terrainById = new();
        readonly Dictionary<string, WorldObjectEntry> _objectById = new();
        readonly Dictionary<string, PoiEntry> _poiById = new();
        readonly Dictionary<string, int> _terrainPriorityById = new();
        readonly Dictionary<string, TerrainTransitionRuleEntry> _transitionRuleByPair = new();
        readonly Dictionary<string, List<TerrainTileCandidate>> _terrainTileCandidatesByCenterTerrain = new();

        public TerrainVisualCatalog TerrainCatalog { get; private set; }
        public TerrainTileSetCatalog TerrainTileSetCatalog { get; private set; }
        public WorldObjectCatalog ObjectCatalog { get; private set; }
        public ZoneRuleCatalog ZoneCatalog { get; private set; }

        public static PCGAssetIndex LoadFromResources(
            string terrainCatalogPath = "PCG/TerrainVisualCatalog",
            string objectCatalogPath = "PCG/WorldObjectCatalog",
            string zoneCatalogPath = "PCG/ZoneRuleCatalog",
            string terrainTileSetCatalogPath = "PCG/TerrainTileSetCatalog")
        {
            var terrainAsset = Resources.Load<TextAsset>(terrainCatalogPath);
            var objectAsset = Resources.Load<TextAsset>(objectCatalogPath);
            var zoneAsset = Resources.Load<TextAsset>(zoneCatalogPath);
            var terrainTileSetAsset = Resources.Load<TextAsset>(terrainTileSetCatalogPath);

            if (terrainAsset == null)
                throw new InvalidOperationException($"PCG terrain catalog not found: Resources/{terrainCatalogPath}.json");
            if (objectAsset == null)
                throw new InvalidOperationException($"PCG object catalog not found: Resources/{objectCatalogPath}.json");
            if (zoneAsset == null)
                throw new InvalidOperationException($"PCG zone catalog not found: Resources/{zoneCatalogPath}.json");

            return FromJson(terrainAsset.text, objectAsset.text, zoneAsset.text, terrainTileSetAsset?.text);
        }

        public static PCGAssetIndex FromJson(string terrainJson, string objectJson, string zoneJson, string terrainTileSetJson = null)
        {
            var index = new PCGAssetIndex
            {
                TerrainCatalog = JsonConvert.DeserializeObject<TerrainVisualCatalog>(terrainJson),
                TerrainTileSetCatalog = string.IsNullOrEmpty(terrainTileSetJson)
                    ? null
                    : JsonConvert.DeserializeObject<TerrainTileSetCatalog>(terrainTileSetJson),
                ObjectCatalog = JsonConvert.DeserializeObject<WorldObjectCatalog>(objectJson),
                ZoneCatalog = JsonConvert.DeserializeObject<ZoneRuleCatalog>(zoneJson),
            };
            index.BuildLookup();
            return index;
        }

        void BuildLookup()
        {
            _terrainById.Clear();
            _objectById.Clear();
            _poiById.Clear();
            _terrainPriorityById.Clear();
            _transitionRuleByPair.Clear();
            _terrainTileCandidatesByCenterTerrain.Clear();

            foreach (var entry in TerrainCatalog.tiles)
            {
                if (!string.IsNullOrEmpty(entry.id))
                    _terrainById[entry.id] = entry;
            }

            foreach (var entry in ObjectCatalog.objects)
            {
                if (!string.IsNullOrEmpty(entry.id))
                    _objectById[entry.id] = entry;
            }

            foreach (var entry in ObjectCatalog.pois)
            {
                if (!string.IsNullOrEmpty(entry.id))
                    _poiById[entry.id] = entry;
            }

            foreach (var entry in TerrainCatalog.terrainPriorities)
            {
                if (!string.IsNullOrEmpty(entry.terrain))
                    _terrainPriorityById[entry.terrain] = entry.priority;
            }

            foreach (var entry in TerrainCatalog.transitionRules)
            {
                if (!string.IsNullOrEmpty(entry.from) && !string.IsNullOrEmpty(entry.to))
                    _transitionRuleByPair[GetTransitionRuleKey(entry.from, entry.to)] = entry;
            }

            BuildTerrainTileCandidateLookup();
        }

        void BuildTerrainTileCandidateLookup()
        {
            var tiles = TerrainTileSetCatalog?.tiles;
            if (tiles == null)
                return;

            foreach (var entry in tiles)
            {
                if (entry == null || string.IsNullOrEmpty(entry.centerTerrain))
                    continue;

                if (!_terrainTileCandidatesByCenterTerrain.TryGetValue(entry.centerTerrain, out var candidates))
                {
                    candidates = new List<TerrainTileCandidate>();
                    _terrainTileCandidatesByCenterTerrain[entry.centerTerrain] = candidates;
                }

                if (entry.orientations != null && entry.orientations.Count > 0)
                {
                    foreach (var orientation in entry.orientations)
                    {
                        if (orientation == null)
                            continue;

                        AddTerrainTileCandidate(
                            candidates,
                            entry,
                            string.IsNullOrEmpty(orientation.asset) ? entry.asset : orientation.asset,
                            orientation.edges,
                            orientation.rotationSteps,
                            orientation.rotationDegrees,
                            orientation.flipX,
                            string.IsNullOrEmpty(orientation.id)
                                ? $"{orientation.rotationSteps}:{orientation.flipX}"
                                : orientation.id);
                    }
                }
                else
                {
                    for (int orientation = 0; orientation < 8; orientation++)
                    {
                        int rotationSteps = orientation & 3;
                        bool flipX = orientation >= 4;
                        AddTerrainTileCandidate(
                            candidates,
                            entry,
                            entry.asset,
                            GetOrientedEdges(entry.edges, rotationSteps, flipX),
                            rotationSteps,
                            -90f * rotationSteps,
                            flipX,
                            $"{rotationSteps}:{flipX}");
                    }
                }
            }
        }

        static void AddTerrainTileCandidate(
            List<TerrainTileCandidate> candidates,
            TerrainTileEntry entry,
            string asset,
            TerrainTileEdges edges,
            int rotationSteps,
            float rotationDegrees,
            bool flipX,
            string orientationKey)
        {
            candidates.Add(new TerrainTileCandidate
            {
                Entry = entry,
                Asset = string.IsNullOrEmpty(asset) ? entry.asset : asset,
                Edges = edges,
                RotationSteps = rotationSteps,
                RotationDegrees = rotationDegrees,
                FlipX = flipX,
                StableHash = StableStringHash32($"{entry.id}:{orientationKey}"),
            });
        }

        public TerrainVisualEntry PickTerrain(string terrain, string useCase, string biome, System.Random rng)
        {
            TerrainVisualEntry fallback = null;
            var total = 0;

            foreach (var entry in TerrainCatalog.tiles)
            {
                if (entry.useCase != useCase || entry.terrain != terrain)
                    continue;

                fallback ??= entry;
                if (!string.IsNullOrEmpty(biome) && entry.biome != biome && entry.biome != "neutral")
                    continue;

                total += Math.Max(1, entry.weight);
            }

            if (total <= 0)
                return fallback ?? TerrainCatalog.tiles.Find(t => t.useCase == useCase) ?? TerrainCatalog.tiles[0];

            var roll = rng.Next(total);
            foreach (var entry in TerrainCatalog.tiles)
            {
                if (entry.useCase != useCase || entry.terrain != terrain)
                    continue;
                if (!string.IsNullOrEmpty(biome) && entry.biome != biome && entry.biome != "neutral")
                    continue;

                roll -= Math.Max(1, entry.weight);
                if (roll < 0)
                    return entry;
            }

            return fallback;
        }

        public TerrainVisualEntry PickByUseCase(string useCase, System.Random rng)
        {
            var total = 0;
            foreach (var entry in TerrainCatalog.tiles)
            {
                if (entry.useCase == useCase)
                    total += Math.Max(1, entry.weight);
            }

            if (total <= 0)
                return null;

            var roll = rng.Next(total);
            foreach (var entry in TerrainCatalog.tiles)
            {
                if (entry.useCase != useCase)
                    continue;

                roll -= Math.Max(1, entry.weight);
                if (roll < 0)
                    return entry;
            }

            return null;
        }

        public int GetTerrainPriority(string terrain)
        {
            if (terrain != null && _terrainPriorityById.TryGetValue(terrain, out int priority))
                return priority;
            return 0;
        }

        public TerrainTransitionRuleEntry GetTransitionRule(string from, string to)
        {
            if (from == null || to == null)
                return null;
            _transitionRuleByPair.TryGetValue(GetTransitionRuleKey(from, to), out var rule);
            return rule;
        }

        public TerrainTransitionMaskEntry PickTransitionMask(string maskSet, string direction, System.Random rng)
        {
            var total = 0;
            foreach (var entry in TerrainCatalog.transitionMasks)
            {
                if (!IsMaskMatch(entry, maskSet, direction))
                    continue;
                total += Math.Max(1, entry.weight);
            }

            if (total <= 0)
                return null;

            var roll = rng.Next(total);
            foreach (var entry in TerrainCatalog.transitionMasks)
            {
                if (!IsMaskMatch(entry, maskSet, direction))
                    continue;
                roll -= Math.Max(1, entry.weight);
                if (roll < 0)
                    return entry;
            }

            return null;
        }

        public TerrainTransitionDetailEntry PickTransitionDetail(string detailSet, System.Random rng)
        {
            var total = 0;
            foreach (var entry in TerrainCatalog.transitionDetails)
            {
                if (entry.detailSet != detailSet)
                    continue;
                total += Math.Max(1, entry.weight);
            }

            if (total <= 0)
                return null;

            var roll = rng.Next(total);
            foreach (var entry in TerrainCatalog.transitionDetails)
            {
                if (entry.detailSet != detailSet)
                    continue;
                roll -= Math.Max(1, entry.weight);
                if (roll < 0)
                    return entry;
            }

            return null;
        }

        public TerrainTilePickResult PickTerrainTile(
            string terrain,
            string northTerrain,
            string eastTerrain,
            string southTerrain,
            string westTerrain,
            TerrainTilePickResult southTile,
            TerrainTilePickResult westTile,
            float tolerance,
            int x,
            int y,
            int seed)
        {
            if (!_terrainTileCandidatesByCenterTerrain.TryGetValue(terrain, out var candidates) || candidates.Count == 0)
                return null;

            tolerance = Mathf.Clamp(tolerance, 0.02f, 0.45f);
            var bestScore = 0f;
            TerrainTilePickResult best = null;
            TerrainTilePickResult fallback = null;
            foreach (var candidate in candidates)
            {
                EvaluateTerrainTileCandidate(
                    candidate,
                    terrain,
                    northTerrain,
                    eastTerrain,
                    southTerrain,
                    westTerrain,
                    southTile,
                    westTile,
                    tolerance,
                    x,
                    y,
                    seed,
                    ref bestScore,
                    ref best,
                    ref fallback);
            }

            return best ?? fallback;
        }

        void EvaluateTerrainTileCandidate(
            TerrainTileCandidate candidate,
            string terrain,
            string northTerrain,
            string eastTerrain,
            string southTerrain,
            string westTerrain,
            TerrainTilePickResult southTile,
            TerrainTilePickResult westTile,
            float tolerance,
            int x,
            int y,
            int seed,
            ref float bestScore,
            ref TerrainTilePickResult best,
            ref TerrainTilePickResult fallback)
        {
            var pick = new TerrainTilePickResult
            {
                Tile = candidate.Entry,
                Asset = candidate.Asset,
                Edges = candidate.Edges,
                RotationSteps = candidate.RotationSteps,
                RotationDegrees = candidate.RotationDegrees,
                FlipX = candidate.FlipX,
            };
            fallback ??= pick;

            float score = Math.Max(1, candidate.Entry.weight);
            if (!AccumulateEdgeScore(candidate.Edges?.north, terrain, northTerrain, tolerance, ref score))
                return;
            if (!AccumulateEdgeScore(candidate.Edges?.east, terrain, eastTerrain, tolerance, ref score))
                return;
            if (!AccumulateEdgeScore(candidate.Edges?.south, terrain, southTerrain, tolerance, ref score))
                return;
            if (!AccumulateEdgeScore(candidate.Edges?.west, terrain, westTerrain, tolerance, ref score))
                return;
            if (!AccumulateAdjacentEdgeScore(candidate.Edges?.south, southTile?.Edges?.north, tolerance, ref score))
                return;
            if (!AccumulateAdjacentEdgeScore(candidate.Edges?.west, westTile?.Edges?.east, tolerance, ref score))
                return;
            if (!AccumulateTerrainRatioScore(candidate.Entry, terrain, northTerrain, eastTerrain, southTerrain, westTerrain, tolerance, ref score))
                return;

            score *= Mathf.Lerp(0.88f, 1.12f, PCGHash.Value01(x, y, seed, candidate.StableHash));
            if (score > bestScore)
            {
                bestScore = score;
                best = pick;
            }
        }

        sealed class TerrainTileCandidate
        {
            public TerrainTileEntry Entry;
            public string Asset;
            public TerrainTileEdges Edges;
            public int RotationSteps;
            public float RotationDegrees;
            public bool FlipX;
            public int StableHash;
        }

        static bool AccumulateTerrainRatioScore(
            TerrainTileEntry entry,
            string terrain,
            string northTerrain,
            string eastTerrain,
            string southTerrain,
            string westTerrain,
            float tolerance,
            ref float score)
        {
            if (entry.terrainRatio == null || string.IsNullOrEmpty(terrain))
                return true;

            int transitionSides = 0;
            if (!string.IsNullOrEmpty(northTerrain) && northTerrain != terrain)
                transitionSides++;
            if (!string.IsNullOrEmpty(eastTerrain) && eastTerrain != terrain)
                transitionSides++;
            if (!string.IsNullOrEmpty(southTerrain) && southTerrain != terrain)
                transitionSides++;
            if (!string.IsNullOrEmpty(westTerrain) && westTerrain != terrain)
                transitionSides++;

            entry.terrainRatio.TryGetValue(terrain, out float localRatio);
            float targetLocalRatio = transitionSides switch
            {
                0 => 0.82f,
                1 => 0.68f,
                2 => 0.54f,
                3 => 0.42f,
                _ => 0.30f,
            };

            float allowedLocalDelta = Mathf.Lerp(0.16f, 0.34f, tolerance / 0.45f);
            float localDelta = Mathf.Abs(localRatio - targetLocalRatio);
            if (localDelta > allowedLocalDelta && tolerance < 0.28f)
                return false;

            score *= Mathf.Lerp(1f, 0.35f, Mathf.Clamp01(localDelta / Math.Max(0.01f, allowedLocalDelta)));

            if (transitionSides == 1 && localRatio < 0.48f && tolerance < 0.34f)
                return false;

            return true;
        }

        static TerrainTileEdges GetOrientedEdges(TerrainTileEdges edges, int clockwiseSteps, bool flipX)
        {
            if (edges == null)
                return null;

            clockwiseSteps = ((clockwiseSteps % 4) + 4) % 4;
            return new TerrainTileEdges
            {
                north = GetOrientedSocket(edges, "north", clockwiseSteps, flipX),
                east = GetOrientedSocket(edges, "east", clockwiseSteps, flipX),
                south = GetOrientedSocket(edges, "south", clockwiseSteps, flipX),
                west = GetOrientedSocket(edges, "west", clockwiseSteps, flipX),
            };
        }

        static TerrainTileEdgeSocket GetOrientedSocket(TerrainTileEdges edges, string direction, int clockwiseSteps, bool flipX)
        {
            string sourceDirection = GetSourceDirectionAfterRotation(direction, clockwiseSteps);
            if (flipX)
                sourceDirection = MirrorSourceDirectionX(sourceDirection);
            var socket = GetSocket(edges, sourceDirection);
            if (socket == null)
                return null;

            return new TerrainTileEdgeSocket
            {
                localTerrain = socket.localTerrain,
                neighborTerrain = socket.neighborTerrain,
                profile = socket.profile,
                signature = clockwiseSteps == 0 && !flipX ? socket.signature : string.Empty,
            };
        }

        static string GetSourceDirectionAfterRotation(string direction, int clockwiseSteps)
        {
            return clockwiseSteps switch
            {
                0 => direction,
                1 => direction switch
                {
                    "north" => "west",
                    "east" => "north",
                    "south" => "east",
                    _ => "south",
                },
                2 => direction switch
                {
                    "north" => "south",
                    "east" => "west",
                    "south" => "north",
                    _ => "east",
                },
                _ => direction switch
                {
                    "north" => "east",
                    "east" => "south",
                    "south" => "west",
                    _ => "north",
                },
            };
        }

        static string MirrorSourceDirectionX(string direction)
        {
            return direction switch
            {
                "east" => "west",
                "west" => "east",
                _ => direction,
            };
        }

        static TerrainTileEdgeSocket GetSocket(TerrainTileEdges edges, string direction)
        {
            return direction switch
            {
                "north" => edges.north,
                "east" => edges.east,
                "south" => edges.south,
                _ => edges.west,
            };
        }

        bool AccumulateAdjacentEdgeScore(
            TerrainTileEdgeSocket candidateSocket,
            TerrainTileEdgeSocket neighborSocket,
            float tolerance,
            ref float score)
        {
            if (candidateSocket == null || neighborSocket == null)
                return true;

            if (string.IsNullOrEmpty(candidateSocket.signature) || !TerrainTileSetCatalog.signatures.TryGetValue(candidateSocket.signature, out var candidateSignature))
                return true;
            if (string.IsNullOrEmpty(neighborSocket.signature) || !TerrainTileSetCatalog.signatures.TryGetValue(neighborSocket.signature, out var neighborSignature))
                return true;

            float compatibility = GetSignatureCompatibility(candidateSocket, candidateSignature, neighborSocket, neighborSignature);
            float signatureInfluence = Mathf.Lerp(0.45f, 0.12f, tolerance / 0.45f);
            score *= Mathf.Lerp(1f - signatureInfluence, 1f + signatureInfluence, compatibility);
            return true;
        }

        bool AccumulateEdgeScore(TerrainTileEdgeSocket socket, string terrain, string neighborTerrain, float tolerance, ref float score)
        {
            if (socket == null || string.IsNullOrEmpty(neighborTerrain))
                return true;

            string expectedPair = GetTerrainPairKey(terrain, neighborTerrain);
            string socketPair = GetTerrainPairKey(socket.localTerrain, socket.neighborTerrain);
            if (socketPair != expectedPair)
            {
                if (tolerance < 0.32f)
                    return false;
                if (socket.localTerrain != terrain && socket.neighborTerrain != terrain)
                    return false;
                score *= 0.25f;
            }
            else
            {
                if (!AccumulateDirectionalEdgeScore(socket, terrain, neighborTerrain, tolerance, ref score))
                    return false;
            }

            if (string.IsNullOrEmpty(socket.signature) || !TerrainTileSetCatalog.signatures.TryGetValue(socket.signature, out var signature))
                return true;

            float fit = GetEdgeTerrainFit(signature, terrain, neighborTerrain);
            float requiredFit = Mathf.Clamp(1f - tolerance * 1.65f, 0.35f, 0.95f);
            if (fit < requiredFit)
                return false;

            score *= Mathf.Lerp(0.35f, 1f, fit);
            return true;
        }

        static bool AccumulateDirectionalEdgeScore(TerrainTileEdgeSocket socket, string terrain, string neighborTerrain, float tolerance, ref float score)
        {
            if (terrain == neighborTerrain)
            {
                if (socket.localTerrain != terrain)
                    score *= Mathf.Lerp(0.35f, 0.8f, tolerance / 0.45f);
                return true;
            }

            bool exact = socket.localTerrain == terrain && socket.neighborTerrain == neighborTerrain;
            if (exact)
            {
                score *= 1.25f;
                return true;
            }

            bool reversed = socket.localTerrain == neighborTerrain && socket.neighborTerrain == terrain;
            if (reversed)
            {
                if (tolerance < 0.30f)
                    return false;
                score *= 0.18f;
                return true;
            }

            if (socket.localTerrain != terrain)
                score *= Mathf.Lerp(0.25f, 0.65f, tolerance / 0.45f);
            if (socket.neighborTerrain != neighborTerrain)
                score *= Mathf.Lerp(0.45f, 0.85f, tolerance / 0.45f);

            return true;
        }

        static float GetSignatureCompatibility(
            TerrainTileEdgeSocket candidateSocket,
            TerrainEdgeSignatureEntry candidateSignature,
            TerrainTileEdgeSocket neighborSocket,
            TerrainEdgeSignatureEntry neighborSignature)
        {
            float colorScore = GetColorSampleScore(candidateSignature.colorSamples, neighborSignature.colorSamples);
            float lumaScore = 1f - Mathf.Clamp01(Mathf.Abs(candidateSignature.lumaMean - neighborSignature.lumaMean) * 2.5f);
            float shapeScore = GetFloatSampleScore(candidateSignature.edgeShape, neighborSignature.edgeShape);
            float terrainRatioScore = GetTerrainRatioScore(candidateSignature.terrainRatio, neighborSignature.terrainRatio);
            float profileScore = candidateSocket.profile == neighborSocket.profile ? 1f : 0.72f;

            return Mathf.Clamp01(
                colorScore * 0.38f +
                lumaScore * 0.18f +
                shapeScore * 0.18f +
                terrainRatioScore * 0.16f +
                profileScore * 0.10f);
        }

        static float GetColorSampleScore(int[][] a, int[][] b)
        {
            if (a == null || b == null || a.Length == 0 || b.Length == 0)
                return 1f;

            int count = Math.Min(a.Length, b.Length);
            float totalDistance = 0f;
            for (int i = 0; i < count; i++)
            {
                if (a[i] == null || b[i] == null || a[i].Length < 3 || b[i].Length < 3)
                    continue;

                float dr = (a[i][0] - b[i][0]) / 255f;
                float dg = (a[i][1] - b[i][1]) / 255f;
                float db = (a[i][2] - b[i][2]) / 255f;
                totalDistance += Mathf.Sqrt(dr * dr + dg * dg + db * db) / 1.732f;
            }

            return 1f - Mathf.Clamp01(totalDistance / count * 1.8f);
        }

        static float GetFloatSampleScore(float[] a, float[] b)
        {
            if (a == null || b == null || a.Length == 0 || b.Length == 0)
                return 1f;

            int count = Math.Min(a.Length, b.Length);
            float totalDistance = 0f;
            for (int i = 0; i < count; i++)
            {
                totalDistance += Mathf.Abs(a[i] - b[i]);
            }

            return 1f - Mathf.Clamp01(totalDistance / count);
        }

        static float GetTerrainRatioScore(Dictionary<string, float> a, Dictionary<string, float> b)
        {
            if (a == null || b == null || a.Count == 0 || b.Count == 0)
                return 1f;

            float totalDistance = 0f;
            int count = 0;
            foreach (var pair in a)
            {
                b.TryGetValue(pair.Key, out float other);
                totalDistance += Mathf.Abs(pair.Value - other);
                count++;
            }

            foreach (var pair in b)
            {
                if (a.ContainsKey(pair.Key))
                    continue;
                totalDistance += Mathf.Abs(pair.Value);
                count++;
            }

            return count == 0 ? 1f : 1f - Mathf.Clamp01(totalDistance / count);
        }

        static float GetEdgeTerrainFit(TerrainEdgeSignatureEntry signature, string terrain, string neighborTerrain)
        {
            if (signature.terrainRatio == null)
                return 1f;

            signature.terrainRatio.TryGetValue(terrain, out float ownRatio);
            if (terrain == neighborTerrain)
                return ownRatio;

            signature.terrainRatio.TryGetValue(neighborTerrain, out float neighborRatio);
            return Mathf.Clamp01(ownRatio + neighborRatio);
        }

        static bool IsMaskMatch(TerrainTransitionMaskEntry entry, string maskSet, string direction)
        {
            if (entry.maskSet != maskSet)
                return false;
            return entry.direction == direction || string.IsNullOrEmpty(entry.direction) || entry.direction == "any";
        }

        static string GetTransitionRuleKey(string from, string to) => $"{from}->{to}";

        static string GetTerrainPairKey(string a, string b)
        {
            a ??= string.Empty;
            b ??= string.Empty;
            return string.CompareOrdinal(a, b) <= 0 ? $"{a}_{b}" : $"{b}_{a}";
        }

        static int StableStringHash32(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                if (!string.IsNullOrEmpty(value))
                {
                    for (int i = 0; i < value.Length; i++)
                    {
                        hash ^= value[i];
                        hash *= 16777619;
                    }
                }
                return (int)(hash & 0x7fffffff);
            }
        }

        public IReadOnlyList<WorldObjectEntry> Objects => ObjectCatalog.objects;
        public IReadOnlyList<PoiEntry> Pois => ObjectCatalog.pois;
    }
}
