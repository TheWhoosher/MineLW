﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using MineLW.API;
using MineLW.Networking;
using MineLW.Protocols.Handshake;
using NLog;

namespace MineLW.Server
{
    public class GameServer : IServer
    {
        public const string Name = "MineLW " + Version;
        private const string Version = "0.1a";
        
        private const uint UpdatePerSecond = 20;
        private const float UpsWarnPercentage = 10 / 100f;
        private const uint UpsWarnThreshold = (uint) (UpdatePerSecond - UpdatePerSecond * UpsWarnPercentage);

        private const float MsPerSecond = 1_000;
        private const float DelayBetweenUpdate = MsPerSecond / UpdatePerSecond;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        private readonly Stopwatch _stopWatch;
        private readonly NetworkServer _networkServer;

        private bool _running;

        public GameServer()
        {
            _stopWatch = new Stopwatch();
            _networkServer = new NetworkServer(HandshakeState.Instance);
        }

        public void Start()
        {
            if (_running)
                return;

            _running = true;
            
            _stopWatch.Start();

            Logger.Info("Starting {0} (Press Ctrl+C to quit)", Name);

            var ipEndPoint = new IPEndPoint(IPAddress.Any, 25565);
            _networkServer.Start(ipEndPoint);

            var elapsed = _stopWatch.ElapsedMilliseconds / MsPerSecond;
            var formattedElapsed = elapsed.ToString("F", CultureInfo.InvariantCulture);
            Logger.Info("Server started in {0}s (running at {1} ups)",formattedElapsed, UpdatePerSecond);

            _stopWatch.Stop();
            
            HandleUpdate();
        }

        private void HandleUpdate()
        {
            var sinceLastUpsCheck = 0f;
            var updateCount = 1;

            try
            {
                while (_running)
                {
                    var elapsedMillis = (float) _stopWatch.ElapsedMilliseconds;
                    sinceLastUpsCheck += elapsedMillis;

                    if (elapsedMillis >= DelayBetweenUpdate)
                    {
                        // restart the timer on the first update call
                        _stopWatch.Restart();

                        do
                        {
                            // compute delta time
                            var deltaTime = elapsedMillis / MsPerSecond;
                        
                            // update elapsed ms
                            elapsedMillis -= DelayBetweenUpdate;

                            // handle update
                            Update(deltaTime);
                            updateCount++;
                        } while (elapsedMillis >= DelayBetweenUpdate);
                    }
                        
                    if (sinceLastUpsCheck > MsPerSecond)
                        continue;
                    
                    // check UPS

                    // handle update per second
                    var updatePerSecond = updateCount / (sinceLastUpsCheck / MsPerSecond);
                    if (updatePerSecond < UpsWarnThreshold)
                    {
                        var ratio = updatePerSecond / (double) UpdatePerSecond;

                        var formattedRatio = (ratio * 100f).ToString("F", CultureInfo.InvariantCulture);
                        var formattedUps = updatePerSecond.ToString("F", CultureInfo.InvariantCulture);

                        Logger.Warn("The server is running at {0}%  ({1} / {2} ups)", formattedRatio,
                            formattedUps,
                            UpdatePerSecond);
                    }

                    sinceLastUpsCheck = 0;
                    updateCount = 1;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Unable to process the update of the server.");
            }
            finally
            {
                try
                {
                    Shutdown();
                }
                catch (Exception e)
                {
                    Logger.Error("An error occurred while stopping the server.\n\n\t-> {0}", e.Message);
                }
            }
        }

        private void Update(float deltaTime)
        {
            _networkServer.Update(deltaTime);
        }

        public void Shutdown()
        {
            if (!_running)
                return;
            
            Logger.Info("Shutting down...");
            
            _networkServer.Stop();
            
            _running = false;
        }
    }
}