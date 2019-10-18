﻿using System;
using MineLW.API;
using MineLW.API.Blocks;
using MineLW.API.Blocks.Palette;
using MineLW.API.Utils;
using MineLW.API.Worlds.Chunks;
using MineLW.Blocks;

namespace MineLW.Worlds.Chunks
{
    public class Chunk : IChunk
    {
        public NBitsArray HeightMap { get; } = NBitsArray.Create(
            9,
            Minecraft.Units.Chunk.Size * Minecraft.Units.Chunk.Size
        );

        /*public ChunkSnapshot Snapshot
        {
            get
            {
                var sectionStorage = new IBlockStorage[Minecraft.Units.Chunk.SectionCount];

                var sectionMask = 0;
                for (var i = 0; i < Minecraft.Units.Chunk.SectionCount; i++)
                {
                    var section = _sections[i];
                    if(section == null)
                        continue;
                    
                    var blockStorage = section.BlockStorage;
                    if (blockStorage.BlockCount == 0)
                        continue;

                    sectionMask |= 1 << i;
                    sectionStorage[i] = blockStorage;
                }

                return new ChunkSnapshot(HeightMap, sectionMask, sectionStorage);
            }
        }*/

        private readonly IBlockPalette _globalPalette;
        private readonly ChunkSection[] _sections = new ChunkSection[Minecraft.Units.Chunk.SectionCount];

        public Chunk(IBlockPalette globalPalette)
        {
            _globalPalette = globalPalette;
        }

        public bool HasSection(int index)
        {
            return _sections[index] != null;
        }

        public IChunkSection CreateSection(int index)
        {
            if (HasSection(index))
                return _sections[index];
            return _sections[index] = new ChunkSection(_globalPalette);
        }

        public void RemoveSection(int index)
        {
            _sections[index] = null;
        }

        public bool HasBlock(int x, int y, int z)
        {
            var index = SectionIndex(y);
            if (!HasSection(index))
                return false;

            var section = _sections[index];
            var blockStorage = section.BlockStorage;
            return blockStorage.HasBlock(x, y / Minecraft.Units.Chunk.SectionHeight, z);
        }

        public void SetBlock(int x, int y, int z, IBlockState blockState)
        {
            var index = SectionIndex(y);
            var section = CreateSection(index);
            var blockStorage = section.BlockStorage;
            blockStorage.SetBlock(
                x, y / Minecraft.Units.Chunk.SectionHeight, z, blockState
            );
        }

        public IBlockState GetBlock(int x, int y, int z)
        {
            var index = SectionIndex(y);
            if (!HasSection(index))
                return BlockState.Air;

            var section = _sections[index];
            var blockStorage = section.BlockStorage;
            return blockStorage.GetBlock(x, y / Minecraft.Units.Chunk.SectionHeight, z);
        }

        public IChunkSection this[int index] => _sections[index];

        public static int SectionIndex(int y)
        {
            var index = (int) Math.Floor((float) y / Minecraft.Units.Chunk.SectionHeight);
            if (0 <= index && index < Minecraft.Units.Chunk.SectionCount)
                return index;
            throw new ArgumentOutOfRangeException(nameof(y), "Invalid Y (" + y + ')');
        }
    }
}