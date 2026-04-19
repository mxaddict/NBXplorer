using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
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

        [Fact]
        public void DeriveBlsctAddress_MatchesFixture()
        {
            if (!HasLibblsct) return;

            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Data", "blsct_vectors.json");
            var doc = JsonDocument.Parse(File.ReadAllText(fixturePath));
            var viewKey  = Encoders.Hex.DecodeData(doc.RootElement.GetProperty("view_key").GetString());
            var spendKey = Encoders.Hex.DecodeData(doc.RootElement.GetProperty("spend_key").GetString());

            foreach (var v in doc.RootElement.GetProperty("vectors").EnumerateArray())
            {
                var account  = v.GetProperty("account").GetInt64();
                var index    = (ulong)v.GetProperty("index").GetInt64();
                var hrp      = v.GetProperty("hrp").GetString();
                var expected = v.GetProperty("address").GetString();

                var actual = BlsctDerivationStrategy.DeriveBlsctAddress(viewKey, spendKey, account, index, hrp);
                Assert.Equal(expected, actual);
            }
        }
    }
}
