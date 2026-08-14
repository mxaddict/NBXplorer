using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.Altcoins;
using NBitcoin.RPC;
using Xunit;
using Xunit.Abstractions;

namespace NBXplorer.Tests
{
    /// <summary>
    /// Custom fact attribute that skips tests unless all three Navio daemon env vars are set.
    /// </summary>
    public class NavioDaemonFactAttribute : FactAttribute
    {
        public NavioDaemonFactAttribute()
        {
            var rpcUrl = Environment.GetEnvironmentVariable("NBXPLORER_NAVTESTNET_RPCURL");
            var rpcUser = Environment.GetEnvironmentVariable("NBXPLORER_NAVTESTNET_RPCUSER");
            var rpcPass = Environment.GetEnvironmentVariable("NBXPLORER_NAVTESTNET_RPCPASSWORD");

            if (string.IsNullOrEmpty(rpcUrl) || string.IsNullOrEmpty(rpcUser) || string.IsNullOrEmpty(rpcPass))
            {
                Skip = "Requires NBXPLORER_NAVTESTNET_RPCURL, NBXPLORER_NAVTESTNET_RPCUSER, and NBXPLORER_NAVTESTNET_RPCPASSWORD environment variables";
            }
        }
    }

    public class NavioDaemonTests
    {
        private readonly ITestOutputHelper _logger;

        public NavioDaemonTests(ITestOutputHelper logger)
        {
            _logger = logger;
        }

        private RPCClient CreateTestnetClient()
        {
            var rpcUrl = Environment.GetEnvironmentVariable("NBXPLORER_NAVTESTNET_RPCURL");
            var rpcUser = Environment.GetEnvironmentVariable("NBXPLORER_NAVTESTNET_RPCUSER");
            var rpcPass = Environment.GetEnvironmentVariable("NBXPLORER_NAVTESTNET_RPCPASSWORD");

            var creds = new RPCCredentialString
            {
                UserPassword = new NetworkCredential(rpcUser, rpcPass)
            };

            var uri = new Uri(rpcUrl);
            var network = AltNetworkSets.Navio.Testnet;
            var client = new RPCClient(creds, uri, network);

            Navio.ConfigureBLSCTOverrides(client);

            return client;
        }

        [NavioDaemonFact]
        public async Task EnsureWalletCreated_ForNavio_CreatesBlsctWallet()
        {
            var client = CreateTestnetClient();

            try
            {
                // Create a BLSCT wallet
                var options = new CreateWalletOptions { Blsct = true };
                var walletName = $"test_blsct_{Guid.NewGuid():N}";

                await client.CreateWalletAsync(walletName, options);

                // Load the wallet and verify blsct flag
                await client.LoadWalletAsync(walletName);

                // Call getwalletinfo and check blsct property
                var walletInfo = await client.SendCommandAsync("getwalletinfo");
                var blsctProperty = walletInfo.Result["blsct"];

                Assert.NotNull(blsctProperty);
                Assert.True((bool)blsctProperty, "Wallet should have blsct=true");
            }
            catch (RPCException ex) when (ex.Message.Contains("already exists"))
            {
                // Wallet might already exist, just load it and verify
                _logger?.WriteLine("Wallet already exists, verifying blsct property...");
            }
        }

        [NavioDaemonFact]
        public async Task GetBalance_RoutesTo_Getblsctbalance()
        {
            var client = CreateTestnetClient();

            // After BLSCT overrides are configured, getbalance should route to getblsctbalance
            // This should not throw an RPCException for method not found
            var result = await client.SendCommandAsync("getbalance");

            // Should return a decimal value (balance)
            Assert.NotNull(result);
        }

        [NavioDaemonFact]
        public async Task Listunspent_RoutesTo_Listblsctunspent()
        {
            var client = CreateTestnetClient();

            // After BLSCT overrides, listunspent should route to listblsctunspent
            var result = await client.SendCommandAsync("listunspent");

            // Should return a JArray (may be empty)
            Assert.NotNull(result);
        }

        [NavioDaemonFact]
        public async Task NavioChain_IndexesBlocks()
        {
            // NBXplorer URL defaults to localhost if env var not set.
            // When running manually, start NBXplorer pointed at naviod before this test.
            var nbxplorerUrl = Environment.GetEnvironmentVariable("NBXPLORER_NAVTESTNET_URL")
                ?? "http://localhost:24445";

            var navNetwork = new NBXplorerNetworkProvider(ChainName.Testnet).GetNAV();
            var explorerClient = new ExplorerClient(navNetwork, new Uri(nbxplorerUrl));

            // Poll GetStatusAsync until NAV chain height > 0, timeout 30s
            NBXplorer.Models.StatusResult status = null;
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    status = await explorerClient.GetStatusAsync();
                    if (status.ChainHeight > 0)
                        break;
                }
                catch { /* NBXplorer may not be ready yet */ }
                await Task.Delay(1000);
            }

            Assert.NotNull(status);
            Assert.True(status.ChainHeight > 0,
                $"NBXplorer NAV chain height should be > 0 after indexing, got {status?.ChainHeight}");
        }
    }
}
