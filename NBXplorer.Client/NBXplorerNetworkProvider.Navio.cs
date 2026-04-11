using NBitcoin;

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
            });
        }

        public NBXplorerNetwork GetNAV()
        {
            return GetFromCryptoCode(
                NBitcoin.Altcoins.Navio.Instance.CryptoCode);
        }
    }
}