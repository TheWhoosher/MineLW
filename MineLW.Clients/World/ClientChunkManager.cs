﻿using System.Collections.Generic;
using MineLW.Adapters;
using MineLW.API;
using MineLW.API.Blocks.Palette;
using MineLW.API.Client;
using MineLW.API.Client.World;
using MineLW.API.Worlds.Chunks;
using MineLW.Blocks.Palette;
using MineLW.Worlds.Chunks;
using NLog;

namespace MineLW.Clients.World
{
    public class ClientChunkManager : IClientChunkManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IClient _client;
        private readonly ISet<ChunkPosition> _loadedChunks = new HashSet<ChunkPosition>();

        public ClientChunkManager(IClient client)
        {
            _client = client;
        }

        public void SynchronizeChunks()
        {
            var clientWorld = _client.World;
            var renderDistance = clientWorld.RenderDistance;
            var clientPosition = clientWorld.ChunkPosition;

            ISet<ChunkPosition> chunkToUnload = new HashSet<ChunkPosition>(_loadedChunks);
            ISet<ChunkPosition> chunkToLoad = new HashSet<ChunkPosition>();

            for (var x = clientPosition.X - renderDistance; x <= clientPosition.X + renderDistance; x++)
            for (var z = clientPosition.Z - renderDistance; z <= clientPosition.Z + renderDistance; z++)
            {
                var chunkPosition = new ChunkPosition(x, z);
                if (chunkToUnload.Contains(chunkPosition))
                    chunkToUnload.Remove(chunkPosition);
                else
                    chunkToLoad.Add(chunkPosition);
            }

            Logger.Debug("Loading {0} chunk(s)", chunkToLoad.Count);
            foreach (var position in chunkToLoad)
                LoadChunk(position);

            Logger.Debug("Unloading {0} chunk(s)", chunkToUnload.Count);
            foreach (var position in chunkToUnload)
                UnloadChunk(position);
        }

        public IChunk RenderChunk(ChunkPosition position, IBlockPalette globalBlockPalette)
        {
            var clientChunk = new Chunk(globalBlockPalette);

            var worldContexts = _client.World.WorldContexts;
            foreach (var worldContext in worldContexts)
            {
                var chunkManager = worldContext.ChunkManager;
                if (!chunkManager.CanGenerate(position) && !chunkManager.IsLoaded(position))
                {
                    Logger.Warn("Skipped chunk at {0}", position);
                    continue;
                }

                var worldChunk = chunkManager.GenerateChunk(position);
                for (var i = 0; i < Minecraft.Units.Chunk.SectionCount; i++)
                {
                    if (!worldChunk.HasSection(i))
                        continue;

                    var worldSection = worldChunk[i];
                    var worldBlockStorage = worldSection.BlockStorage;
                    if (worldBlockStorage.BlockCount == 0)
                        continue;

                    var clientSection = clientChunk.CreateSection(i);
                    var clientBlockStorage = clientSection.BlockStorage;

                    for (var x = 0; x < Minecraft.Units.Chunk.Size; x++)
                    for (var z = 0; z < Minecraft.Units.Chunk.Size; z++)
                    for (var y = 0; y < Minecraft.Units.Chunk.SectionHeight; y++)
                    {
                        var worldBlock = worldBlockStorage.GetBlock(x, y, z);
                        if(worldBlock != null)
                            clientBlockStorage.SetBlock(x, y, z, worldBlock);
                    }
                }
            }

            return clientChunk;
        }

        public bool IsLoaded(ChunkPosition position)
        {
            return _loadedChunks.Contains(position);
        }

        public void LoadChunk(ChunkPosition position)
        {
            if (!_loadedChunks.Add(position))
            {
                Logger.Warn("Trying to load an already loaded chunk at {0}", position);
                return;
            }

            // TODO use Client's adapter
            var version = GameAdapters.ServerVersion;
            var adapter = GameAdapters.Resolve(version.Protocol);
            var renderedChunk = RenderChunk(position, new GlobalBlockPalette(adapter.BlockManager));
            _client.Connection.LoadChunk(position, renderedChunk);
        }

        public void UnloadChunk(ChunkPosition position)
        {
            if (!_loadedChunks.Remove(position))
            {
                Logger.Warn("Trying to unload an unloaded chunk at {0}", position);
                return;
            }

            _client.Connection.UnloadChunk(position);
        }
    }
}