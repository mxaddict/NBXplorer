using System;
using NBitcoin;
using NBXplorer;
using NBXplorer.DerivationStrategy;
using Xunit;

namespace NBXplorer.Tests
{
    public class NavioTests
    {
        const string ValidView = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        const string ValidSpend = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        const string ValidStr = "blsct:" + ValidView + ":" + ValidSpend;

        [Fact]
        public void BlsctDerivationStrategyParseValidString()
        {
            var strategy = BlsctDerivationStrategy.Parse(ValidStr);
            Assert.NotNull(strategy);
            Assert.Equal(32, strategy.ViewKey.Length);
            Assert.Equal(48, strategy.SpendKey.Length);
        }

        [Fact]
        public void BlsctDerivationStrategyParseViewKeyMatchesInput()
        {
            var strategy = BlsctDerivationStrategy.Parse(ValidStr);
            var expectedViewKey = NBitcoin.DataEncoders.Encoders.Hex.DecodeData(ValidView);
            Assert.Equal(expectedViewKey, strategy.ViewKey);
        }

        [Fact]
        public void BlsctDerivationStrategyParseSpendKeyMatchesInput()
        {
            var strategy = BlsctDerivationStrategy.Parse(ValidStr);
            var expectedSpendKey = NBitcoin.DataEncoders.Encoders.Hex.DecodeData(ValidSpend);
            Assert.Equal(expectedSpendKey, strategy.SpendKey);
        }

        [Fact]
        public void BlsctDerivationStrategyParseInvalidViewKeyTooShort()
        {
            var str = "blsct:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa:" + ValidSpend;
            var strategy = BlsctDerivationStrategy.Parse(str);
            Assert.Null(strategy);
        }

        [Fact]
        public void BlsctDerivationStrategyParseInvalidSpendKeyTooShort()
        {
            var str = "blsct:" + ValidView + ":bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
            var strategy = BlsctDerivationStrategy.Parse(str);
            Assert.Null(strategy);
        }

        [Fact]
        public void BlsctDerivationStrategyParseMissingPrefix()
        {
            var str = ValidView + ":" + ValidSpend;
            var strategy = BlsctDerivationStrategy.Parse(str);
            Assert.Null(strategy);
        }

        [Fact]
        public void BlsctDerivationStrategyParseNonHexCharactersInViewKey()
        {
            var str = "blsct:gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg:" + ValidSpend;
            var strategy = BlsctDerivationStrategy.Parse(str);
            Assert.Null(strategy);
        }

        [Fact]
        public void BlsctDerivationStrategyParseNullInput()
        {
            var strategy = BlsctDerivationStrategy.Parse(null);
            Assert.Null(strategy);
        }

        [Fact]
        public void BlsctDerivationStrategyToStringRoundTrip()
        {
            var strategy = BlsctDerivationStrategy.Parse(ValidStr);
            var result = strategy.ToString();
            Assert.Equal(ValidStr, result);
        }

        [Fact]
        public void BlsctDerivationStrategyFactoryParseValidString()
        {
            var network = NBitcoin.Altcoins.AltNetworkSets.Navio.Testnet;
            var factory = new BlsctDerivationStrategyFactory(network);
            var result = factory.Parse(ValidStr);
            Assert.NotNull(result);
            Assert.IsType<BlsctDerivationStrategy>(result);
        }

        [Fact]
        public void BlsctDerivationStrategyFactoryParseKeysMatch()
        {
            var network = NBitcoin.Altcoins.AltNetworkSets.Navio.Testnet;
            var factory = new BlsctDerivationStrategyFactory(network);
            var result = (BlsctDerivationStrategy)factory.Parse(ValidStr);

            var expectedViewKey = NBitcoin.DataEncoders.Encoders.Hex.DecodeData(ValidView);
            var expectedSpendKey = NBitcoin.DataEncoders.Encoders.Hex.DecodeData(ValidSpend);

            Assert.Equal(expectedViewKey, result.ViewKey);
            Assert.Equal(expectedSpendKey, result.SpendKey);
        }

        [Fact]
        public void NavioNBXplorerNetworkCryptoCodeIsNav()
        {
            var provider = new NBXplorerNetworkProvider(NBitcoin.ChainName.Testnet);
            var navNetwork = provider.GetNAV();
            Assert.NotNull(navNetwork);
            Assert.Equal("NAV", navNetwork.CryptoCode);
        }

        [Fact]
        public void NavioNBXplorerNetworkMinRPCVersionIs220000()
        {
            var provider = new NBXplorerNetworkProvider(NBitcoin.ChainName.Testnet);
            var navNetwork = provider.GetNAV();
            Assert.Equal(220000, navNetwork.MinRPCVersion);
        }

        [Fact]
        public void NavioNBXplorerNetworkDerivationStrategyFactoryIsBlsct()
        {
            var provider = new NBXplorerNetworkProvider(NBitcoin.ChainName.Testnet);
            var navNetwork = provider.GetNAV();
            var factory = navNetwork.DerivationStrategyFactory;
            Assert.NotNull(factory);
            Assert.IsType<BlsctDerivationStrategyFactory>(factory);
        }

        [Fact]
        public void NavioNBXplorerNetworkTestnetCoinTypeIsOne()
        {
            var provider = new NBXplorerNetworkProvider(NBitcoin.ChainName.Testnet);
            var navNetwork = provider.GetNAV();
            var expectedCoinType = new NBitcoin.KeyPath("1'");
            Assert.Equal(expectedCoinType, navNetwork.CoinType);
        }

        [Fact]
        public void NavioNBXplorerNetworkMainnetCoinTypeIsZero()
        {
            var provider = new NBXplorerNetworkProvider(NBitcoin.ChainName.Mainnet);
            var navNetwork = provider.GetNAV();
            var expectedCoinType = new NBitcoin.KeyPath("0'");
            Assert.Equal(expectedCoinType, navNetwork.CoinType);
        }

        [Fact]
        public void BlsctDerivationStrategyFactory_Parse_NonBlsctString_FallsBackToBase()
        {
            var network = NBitcoin.Altcoins.AltNetworkSets.Navio.Testnet;
            var factory = new BlsctDerivationStrategyFactory(network);

            // Generate a valid HD pubkey in testnet format so the base class can parse it.
            // This confirms the factory falls back correctly for non-BLSCT inputs.
            var xpub = new NBitcoin.ExtKey().Neuter().ToString(network);
            var result = factory.Parse(xpub);

            Assert.NotNull(result);
            Assert.False(result is BlsctDerivationStrategy);
        }
    }
}
