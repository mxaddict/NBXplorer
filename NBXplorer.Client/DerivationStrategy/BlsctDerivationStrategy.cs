using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using NavioBlsct;
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
    /// which calls the NavioBlsct SWIG bindings.
    /// </summary>
    public class BlsctDerivationStrategy : DerivationStrategyBase
    {
        public const string Prefix = "blsct:";
        public const long ChangeAccount = -1;
        public const long StakingAccount = -2;

        public byte[] ViewKey { get; }
        public byte[] SpendKey { get; }

        [DllImport("blsct", EntryPoint = "free_obj", CallingConvention = CallingConvention.Cdecl)]
        private static extern void NativeFreeObj(IntPtr ptr);

        private static void FreeSwigObj(object obj)
        {
            if (obj is null) return;
            var field = obj.GetType().GetField("swigCPtr", BindingFlags.Instance | BindingFlags.NonPublic);
            var raw = field?.GetValue(obj);
            if (raw is HandleRef hr)
            {
                NativeFreeObj(hr.Handle);
                var cm = obj.GetType().GetField("swigCMemOwn", BindingFlags.Instance | BindingFlags.NonPublic);
                cm?.SetValue(obj, false);
            }
        }

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
        /// Derives a BLSCT address using the SWIG-generated NavioBlsct bindings.
        /// </summary>
        public static string DeriveBlsctAddress(byte[] viewKey, byte[] spendKey, long account, ulong index, string hrp)
        {
            blsct.init();

            var chain = hrp switch
            {
                "nav" => BlsctChain.Mainnet,
                _     => BlsctChain.Testnet,
            };
            blsct.set_blsct_chain(chain);

            var viewKeyHex  = Encoders.Hex.EncodeData(viewKey);
            var spendKeyHex = Encoders.Hex.EncodeData(spendKey);

            var rvVk = blsct.deserialize_scalar(viewKeyHex);
            if (rvVk is null || rvVk.result != 0)
                throw new InvalidOperationException("Failed to deserialize view key");
            var viewKeyScalar = blsct.cast_to_scalar(rvVk.value);

            var rvSk = blsct.deserialize_public_key(spendKeyHex);
            if (rvSk is null || rvSk.result != 0)
                throw new InvalidOperationException("Failed to deserialize spend key");
            var spendPubKey = blsct.cast_to_pub_key(rvSk.value);

            // gen_dpk_with_keys_acct_addr encapsulates gen_sub_addr_id + derive_sub_address + sub_addr_to_dpk
            var dpk = blsct.gen_dpk_with_keys_acct_addr(viewKeyScalar, spendPubKey, account, index);

            // encode_address needs SWIGTYPE_p_void; roundtrip via serialize/deserialize to get BlsctRetVal.value
            var dpkHex = blsct.serialize_dpk(dpk);
            var rvDpk  = blsct.deserialize_dpk(dpkHex);
            if (rvDpk is null || rvDpk.result != 0)
                throw new InvalidOperationException("Failed to roundtrip dpk for encoding");

            var rvEnc = blsct.encode_address(rvDpk.value, AddressEncoding.Bech32M);
            if (rvEnc is null || rvEnc.result != 0)
                throw new InvalidOperationException("Failed to encode BLSCT address");

            var addrStr = blsct.cast_to_const_char_ptr(rvEnc.value)
                ?? throw new InvalidOperationException("Failed to read encoded address string");

            FreeSwigObj(rvVk.value);
            FreeSwigObj(rvSk.value);
            FreeSwigObj(dpk);
            FreeSwigObj(rvDpk.value);
            FreeSwigObj(rvEnc.value);

            return addrStr;
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
