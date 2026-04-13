using NBitcoin;
using NBXplorer.DerivationStrategy;

namespace NBXplorer
{
    public partial class NBXplorerNetworkProvider
    {
        public class NavioNBXplorerNetwork : NBXplorerNetwork
        {
            internal NavioNBXplorerNetwork(INetworkSet networkSet, ChainName networkType) : base(networkSet, networkType)
            {
            }

            internal override DerivationStrategyFactory CreateStrategyFactory()
            {
                return new BlsctDerivationStrategyFactory(NBitcoinNetwork);
            }
        }

        private void InitNavio(ChainName networkType)
        {
            Add(new NavioNBXplorerNetwork(NBitcoin.Altcoins.Navio.Instance, networkType)
            {
                MinRPCVersion = 220000,
                CoinType = networkType == ChainName.Mainnet
                    ? new KeyPath("0'")
                    : new KeyPath("1'"),
            });
        }

        public NBXplorerNetwork GetNAV()
        {
            return GetFromCryptoCode(
                NBitcoin.Altcoins.Navio.Instance.CryptoCode);
        }
    }
}
