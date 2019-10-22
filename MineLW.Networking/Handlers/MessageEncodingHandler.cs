﻿using System.Collections.Generic;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using MineLW.Networking.IO;
using MineLW.Networking.Messages;
using NLog;

namespace MineLW.Networking.Handlers
{
    public class MessageEncodingHandler : MessageToMessageCodec<IByteBuffer, IMessage>
    {
        public const string Name = "message_encoding";
        
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly NetworkClient _client;

        public MessageEncodingHandler(NetworkClient client)
        {
            _client = client;
        }

        protected override void Encode(IChannelHandlerContext ctx, IMessage msg, List<object> output)
        {
            var buffer = ctx.Allocator.Buffer();
            
            var state = _client.State;
            state.Serialize(buffer, msg);
            
            Logger.Debug("Sending message \"{0}\" to {1} ({2} bytes)", msg, _client, buffer.ReadableBytes);

            output.Add(buffer);
        }

        protected override void Decode(IChannelHandlerContext ctx, IByteBuffer msg, List<object> output)
        {
            var state = _client.State;
            var id = msg.ReadVarInt32();

            try
            {
                var deserializer = state.GetDeserializer(id);
                if (deserializer == null)
                {
#if DEBUG
                    msg.SkipBytes(msg.ReadableBytes);
                    Logger.Debug("Undefined message {0} in state {1}", id, state);
#endif
                    return;
                }
                
                var message = deserializer.Deserialize(msg);
                if (msg.ReadableBytes > 0)
                    throw new DecoderException("Too many bytes");

                output.Add(message);

                Logger.Debug("Receiving message \"{0}\" from {1}", message, _client);
            }
            catch (DecoderException e)
            {
                throw new DecoderException("Unable to decode message id " + id, e);
            }
        }
    }
}