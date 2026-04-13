using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NBitcoin;
using NBitcoin.DataEncoders;

namespace NBXplorer.DerivationStrategy
{
    /// <summary>
    /// NBXplorer DerivationStrategyBase for BLSCT wallets.
    /// String format: "blsct:VIEW_KEY_HEX:SPEND_KEY_HEX"
    /// 
    /// Note: This strategy is only used for parsing and identification.
    /// Actual address derivation is handled by GenerateBlsctAddress static method
    /// which calls the native libblsct library directly.
    /// </summary>
    public class BlsctDerivationStrategy : DerivationStrategyBase
    {
        public const string Prefix = "blsct:";
        public const long ChangeAccount = -1;
        public const long StakingAccount = -2;

        public byte[] ViewKey { get; }
        public byte[] SpendKey { get; }

        // Native library imports for BLSCT address derivation
        private const string LibName = "blsct";

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gen_sub_addr_id(long account, ulong address);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe IntPtr derive_sub_address(byte* viewKey, byte* spendKey, IntPtr subAddrId);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr encode_address(IntPtr subAddr, int encoding);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void free_obj(IntPtr obj);

        public BlsctDerivationStrategy(byte[] viewKey, byte[] spendKey)
            : base(null)
        {
            if (viewKey.Length != 32) throw new ArgumentException("View key must be 32 bytes");
            if (spendKey.Length != 48) throw new ArgumentException("Spend key must be 48 bytes");
            ViewKey = viewKey;
            SpendKey = spendKey;
        }

        public static BlsctDerivationStrategy Parse(string s)
        {
            if (!s.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)) return null;
            var parts = s.Substring(Prefix.Length).Split(':');
            if (parts.Length != 2 || parts[0].Length != 64 || parts[1].Length != 96) return null;
            try
            {
                return new BlsctDerivationStrategy(
                    Encoders.Hex.DecodeData(parts[0]),
                    Encoders.Hex.DecodeData(parts[1]));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Derives a BLSCT address using the native libblsct library.
        /// </summary>
        public static unsafe string DeriveBlsctAddress(byte[] viewKey, byte[] spendKey, long account, ulong index, string hrp)
        {
            IntPtr id = IntPtr.Zero;
            IntPtr addr = IntPtr.Zero;
            IntPtr retVal = IntPtr.Zero;
            try
            {
                id = gen_sub_addr_id(account, index);
                fixed (byte* vk = viewKey, sk = spendKey)
                {
                    addr = derive_sub_address(vk, sk, id);
                }
                retVal = encode_address(addr, 1); // 1 = Bech32M
                var strPtr = Marshal.ReadIntPtr(retVal, IntPtr.Size);
                return Marshal.PtrToStringAnsi(strPtr) ?? throw new InvalidOperationException("Failed to encode address");
            }
            finally
            {
                if (retVal != IntPtr.Zero) free_obj(retVal);
                if (addr != IntPtr.Zero) free_obj(addr);
                if (id != IntPtr.Zero) free_obj(id);
            }
        }

        protected internal override string StringValueCore =>
            Prefix + Encoders.Hex.EncodeData(ViewKey) + ":" + Encoders.Hex.EncodeData(SpendKey);

        public override IEnumerable<ExtPubKey> GetExtPubKeys() =>
            Array.Empty<ExtPubKey>();

        public override DerivationLine GetLineFor(KeyPathTemplates keyPathTemplates, DerivationFeature feature) =>
            throw new NotSupportedException("BLSCT uses Repository.GenerateBlsctAddressesCore for address derivation");
    }

    /// <summary>
    /// Factory for parsing BLSCT derivation strategy strings.
    /// </summary>
    public class BlsctDerivationStrategyFactory : DerivationStrategyFactory
    {
        public BlsctDerivationStrategyFactory(Network network) : base(network)
        {
        }

        public new DerivationStrategyBase Parse(string str)
        {
            var blsctStrategy = BlsctDerivationStrategy.Parse(str);
            if (blsctStrategy != null)
                return blsctStrategy;

            return base.Parse(str);
        }
    }
}
