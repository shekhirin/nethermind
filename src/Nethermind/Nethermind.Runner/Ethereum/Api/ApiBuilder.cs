// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Autofac;
using Autofac.Core;
using Nethermind.Api;
using Nethermind.Api.Extensions;
using Nethermind.Config;
using Nethermind.Consensus;
using Nethermind.Core;
using Nethermind.Init;
using Nethermind.JsonRpc.Data;
using Nethermind.Logging;
using Nethermind.Serialization.Json;
using Nethermind.Specs.ChainSpecStyle;
using ILogger = Nethermind.Logging.ILogger;

namespace Nethermind.Runner.Ethereum.Api
{
    public class ApiBuilder
    {
        private readonly IConfigProvider _configProvider;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogManager _logManager;
        private readonly ILogger _logger;
        private readonly IInitConfig _initConfig;

        public ApiBuilder(IConfigProvider configProvider, ILogManager logManager)
        {
            _logManager = logManager ?? throw new ArgumentNullException(nameof(logManager));
            _logger = _logManager.GetClassLogger();
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _initConfig = configProvider.GetConfig<IInitConfig>();
            _jsonSerializer = new EthereumJsonSerializer();
        }

        public INethermindApi Create(params INethermindPlugin[] consensusPlugins) =>
            Create((IEnumerable<INethermindPlugin>)consensusPlugins);

        public INethermindApi Create(IEnumerable<INethermindPlugin> plugins)
        {
            ChainSpec chainSpec = LoadChainSpec(_jsonSerializer);
            bool wasCreated = Interlocked.CompareExchange(ref _apiCreated, 1, 0) == 1;
            if (wasCreated)
            {
                throw new NotSupportedException("Creation of multiple APIs not supported.");
            }

            string engine = chainSpec.SealEngineType;
            IConsensusPlugin? enginePlugin = (IConsensusPlugin)plugins.FirstOrDefault(p => p is IConsensusPlugin consensus && consensus.SealEngineType == engine);

            INethermindApi nethermindApi =
                enginePlugin?.CreateApi(_configProvider, _jsonSerializer, _logManager, chainSpec) ??
                new NethermindApi(_configProvider, _jsonSerializer, _logManager, chainSpec);
            nethermindApi.SealEngineType = engine;
            nethermindApi.SpecProvider = new ChainSpecBasedSpecProvider(chainSpec, _logManager);
            nethermindApi.GasLimitCalculator = new FollowOtherMiners(nethermindApi.SpecProvider);
            ((List<INethermindPlugin>)nethermindApi.Plugins).AddRange(plugins);

            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterModule(new CoreModule(nethermindApi, _configProvider, _jsonSerializer, _logManager));

            foreach (INethermindPlugin nethermindPlugin in plugins)
            {
                if (nethermindPlugin is IModule autofacModule)
                {
                    builder.RegisterModule(autofacModule);
                }
            }

            nethermindApi.Container = builder.Build();

            SetLoggerVariables(chainSpec);

            return nethermindApi;
        }

        private int _apiCreated;

        private ChainSpec LoadChainSpec(IJsonSerializer ethereumJsonSerializer)
        {
            bool hiveEnabled = Environment.GetEnvironmentVariable("NETHERMIND_HIVE_ENABLED")?.ToLowerInvariant() == "true";
            bool hiveChainSpecExists = File.Exists(_initConfig.HiveChainSpecPath);

            string chainSpecFile;
            if (hiveEnabled && hiveChainSpecExists)
                chainSpecFile = _initConfig.HiveChainSpecPath;
            else
                chainSpecFile = _initConfig.ChainSpecPath;

            if (_logger.IsDebug) _logger.Debug($"Loading chain spec from {chainSpecFile}");

            ThisNodeInfo.AddInfo("Chainspec    :", $"{chainSpecFile}");

            IChainSpecLoader loader = new ChainSpecLoader(ethereumJsonSerializer);
            ChainSpec chainSpec = loader.LoadEmbeddedOrFromFile(chainSpecFile, _logger);
            TransactionForRpc.DefaultChainId = chainSpec.ChainId;
            return chainSpec;
        }

        private void SetLoggerVariables(ChainSpec chainSpec)
        {
            _logManager.SetGlobalVariable("chain", chainSpec.Name);
            _logManager.SetGlobalVariable("chainId", chainSpec.ChainId);
            _logManager.SetGlobalVariable("engine", chainSpec.SealEngineType);
        }
    }
}
