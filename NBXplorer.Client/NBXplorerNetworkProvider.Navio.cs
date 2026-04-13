using NBitcoin;
using NBXplorer.DerivationStrategy;

namespace NBXplorer
{
    public partial class NBXplorerNetworkProvider
    {
        private void InitNavio(ChainName networkType)
        {
            Add(new NBXplorerNetwork(NBitcoin.Altcoins.Navio.Instance, networkType)
            {
                MinRPCVersion = 220000,
                CoinType = networkType == ChainName.Mainnet
                    ? new KeyPath("0'")
                    : new KeyPath("1'"),
                // Custom strategy parser: recognize "blsct:..." strings
                DerivationStrategyFactory = new BlsctDerivationStrategyFactory(NBitcoin.Altcoins.Navio.Instance.GetNetwork(networkType)),
            });
        }

        public NBXplorerNetwork GetNAV()
        {
            return GetFromCryptoCode(
                NBitcoin.Altcoins.Navio.Instance.CryptoCode);
        }
    }
}