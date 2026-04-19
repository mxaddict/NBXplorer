using System;
using System.Runtime.InteropServices;
using NBitcoin.DataEncoders;
using NBXplorer.DerivationStrategy;
using Xunit;

namespace NBXplorer.Tests
{
    public class BlsctDerivationTests
    {
        // Valid BLS12-381 keys derived from seed scalar=1 via from_seed_to_child_key chain.
        // All-zero bytes crash deserialize_public_key; these are real valid curve points.
        static readonly byte[] TestViewKey  = Encoders.Hex.DecodeData("3da4775835415f0b77e92be41c57d0f96e484d6367755c3bc645d3f37077d0cc");
        static readonly byte[] TestSpendKey = Encoders.Hex.DecodeData("887e0f73b4b2395838b63ca6539ecfc885c8d02a97d6ef36904cbac71b07e3f5d2dc6800410fa9ece530076f577670b6");

        static BlsctDerivationTests()
        {
            var libPath = Environment.GetEnvironmentVariable("LIBBLSCT_SO_PATH");
            if (!string.IsNullOrEmpty(libPath))
            {
                var handle = NativeLibrary.Load(libPath);
                IntPtr Resolver(string name, System.Reflection.Assembly _, DllImportSearchPath? __)
                    => name == "blsct" ? handle : IntPtr.Zero;
                NativeLibrary.SetDllImportResolver(typeof(NavioBlsct.blsct).Assembly, Resolver);
                NativeLibrary.SetDllImportResolver(typeof(BlsctDerivationStrategy).Assembly, Resolver);
            }
        }

        private static bool HasLibblsct => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LIBBLSCT_SO_PATH"));

        [Fact]
        public void DeriveBlsctAddressWithTnvHrpStartsWithPrefix()
        {
            if (!HasLibblsct) return;

            var address = BlsctDerivationStrategy.DeriveBlsctAddress(TestViewKey, TestSpendKey, 0, 0, "tnv");
            Assert.StartsWith("tnv1", address);
        }

        // NOTE: DeriveBlsctAddress accepts an `hrp` parameter but the underlying
        // C encode_address(BlsctSubAddr*, AddressEncoding) does not take an HRP argument.
        // The native library encodes with a fixed HRP baked into the sub-address structure.
        // HRP switching (nav vs tnv) is a TODO in BlsctDerivationStrategy.
        // A "nav1" prefix test is omitted until the hrp param is wired through.

        [Fact]
        public void DeriveBlsctAddressIsDeterministic()
        {
            if (!HasLibblsct) return;

            var address1 = BlsctDerivationStrategy.DeriveBlsctAddress(TestViewKey, TestSpendKey, 0, 0, "tnv");
            var address2 = BlsctDerivationStrategy.DeriveBlsctAddress(TestViewKey, TestSpendKey, 0, 0, "tnv");
            Assert.Equal(address1, address2);
        }

        [Fact]
        public void DeriveBlsctAddressDifferentAccountProducesDifferentAddress()
        {
            if (!HasLibblsct) return;

            var address1 = BlsctDerivationStrategy.DeriveBlsctAddress(TestViewKey, TestSpendKey, 0, 0, "tnv");
            var address2 = BlsctDerivationStrategy.DeriveBlsctAddress(TestViewKey, TestSpendKey, 1, 0, "tnv");
            Assert.NotEqual(address1, address2);
        }

        [Fact]
        public void DeriveBlsctAddressDifferentIndexProducesDifferentAddress()
        {
            if (!HasLibblsct) return;

            var address1 = BlsctDerivationStrategy.DeriveBlsctAddress(TestViewKey, TestSpendKey, 0, 0, "tnv");
            var address2 = BlsctDerivationStrategy.DeriveBlsctAddress(TestViewKey, TestSpendKey, 0, 1, "tnv");
            Assert.NotEqual(address1, address2);
        }

        [Fact]
        public void DeriveBlsctAddressChangeAccountProducesDifferentAddress()
        {
            if (!HasLibblsct) return;

            var address1 = BlsctDerivationStrategy.DeriveBlsctAddress(TestViewKey, TestSpendKey, 0, 0, "tnv");
            var address2 = BlsctDerivationStrategy.DeriveBlsctAddress(TestViewKey, TestSpendKey, BlsctDerivationStrategy.ChangeAccount, 0, "tnv");
            Assert.NotEqual(address1, address2);
        }

        [Fact]
        public void ChangeAccountConstantIsNegativeOne()
        {
            Assert.Equal(-1L, BlsctDerivationStrategy.ChangeAccount);
        }

        [Fact(Skip = "fixture not yet generated")]
        public void DeriveBlsctAddress_MatchesFixture()
        {
            // Test vectors are loaded from blsct_vectors.json (not yet generated).
            // Once libblsct.so builds and the fixture generator runs, this test will
            // verify that DeriveBlsctAddress produces the expected addresses.
            // See BTCPAY.md "Test Vectors" section for fixture generation steps.
        }
    }
}
