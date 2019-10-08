﻿using System;
using System.ComponentModel;

namespace MineLW.Blocks.Properties
{
    public class BlockPropertyEnum : BlockProperty<string>
    {
        public BlockPropertyEnum(string name, params string[] values) : base(name, Array.AsReadOnly(values))
        {
        }

        public override object Parse(string source)
        {
            foreach (var value in Values)
            {
                if (value.Equals(source, StringComparison.Ordinal))
                    return value;
            }
            
            throw new InvalidEnumArgumentException("Undefined enum value \"" + source + "\"");
        }
    }
}