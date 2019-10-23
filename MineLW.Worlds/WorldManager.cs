﻿using System;
using System.Collections.Generic;
using System.Threading;
using MineLW.API;
using MineLW.API.Blocks;
using MineLW.API.Blocks.Palette;
using MineLW.API.Utils;
using MineLW.API.Worlds;
using MineLW.API.Worlds.Events;
using MineLW.Blocks.Palette;

namespace MineLW.Worlds
{
    public class WorldManager : IWorldManager
    {
        public Identifier DefaultWorld { get; } = Minecraft.CreateIdentifier("default");
        
        public IBlockManager BlockManager { get; }

        public event EventHandler<WorldEventArgs> WorldCreated;

        private readonly IBlockPalette _globalPalette;
        private readonly Dictionary<Identifier, IWorld> _worlds = new Dictionary<Identifier, IWorld>();

        private int _uniqueId;
        
        public WorldManager(IBlockManager blockManager)
        {
            BlockManager = blockManager;
            _globalPalette = new GlobalBlockPalette(blockManager);
        }
        
        public IWorld CreateWorld(Identifier name)
        {
            if (_worlds.ContainsKey(name))
                return _worlds[name];

            var world = new World(_globalPalette);
            WorldCreated?.Invoke(this, new WorldEventArgs(world));
            return _worlds[name] = world;
        }

        public int RequestUniqueId()
        {
            return Interlocked.Increment(ref _uniqueId);
        }

        public IWorld this[Identifier name] => _worlds[name];
    }
}