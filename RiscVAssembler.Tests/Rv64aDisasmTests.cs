using System;
using FluentAssertions;
using RiscVAssembler.Decoder;
using RiscVAssembler.RiscV;
using Xunit;

namespace RiscVAssembler.Tests;

public class Rv64aDisasmTests
{
    private static string Disasm(string hex)
    {
        var s = hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? hex[2..] : hex;
        uint v = Convert.ToUInt32(s, 16);
        return new UnifiedDisassembler(Xlen.X64).Disassemble(v);
    }

    [Fact]
    public void Rv64a_LrSc()
    {
        Disasm("100130af").Should().Contain("lr.d");
        Disasm("183130af").Should().Contain("sc.d");
    }

    [Fact]
    public void Rv64a_Amoswap()
    {
        Disasm("083130af").Should().Contain("amoswap.d");
    }

    [Theory]
    [InlineData("003130af", "amoadd.d")]
    [InlineData("203130af", "amoxor.d")]
    [InlineData("603130af", "amoand.d")]
    [InlineData("403130af", "amoor.d")]
    [InlineData("803130af", "amomin.d")]
    [InlineData("a03130af", "amomax.d")]
    [InlineData("c03130af", "amominu.d")]
    [InlineData("e03130af", "amomaxu.d")]
    public void Rv64a_Arithmetic(string hex, string mnemonic)
    {
        Disasm(hex).Should().Contain(mnemonic);
    }

    [Fact]
    public void Rv64a_AcquireRelease_FlagsIgnoredButMnemonicPresent()
    {
        Disasm("140130af").Should().Contain("lr.d");   // aq
        Disasm("1a3130af").Should().Contain("sc.d");   // rl
        Disasm("0e3130af").Should().Contain("amoswap.d"); // aqrl
    }

    [Theory]
    [InlineData("0050baaf", "amoadd.d")]
    [InlineData("100f30af", "lr.d")]
    public void Rv64a_VariousRegisters(string hex, string mnemonic)
    {
        Disasm(hex).Should().Contain(mnemonic);
    }

    [Theory]
    [InlineData("100130af", "lr.d")]
    [InlineData("183130af", "sc.d")]
    [InlineData("083130af", "amoswap.d")]
    [InlineData("003130af", "amoadd.d")]
    [InlineData("203130af", "amoxor.d")]
    [InlineData("603130af", "amoand.d")]
    [InlineData("403130af", "amoor.d")]
    [InlineData("803130af", "amomin.d")]
    [InlineData("a03130af", "amomax.d")]
    [InlineData("c03130af", "amominu.d")]
    [InlineData("e03130af", "amomaxu.d")]
    public void Rv64a_Comprehensive(string hex, string mnemonic)
    {
        Disasm(hex).Should().Contain(mnemonic);
    }

    [Theory]
    [InlineData("f83130af")] // invalid funct5
    [InlineData("003140af")] // invalid funct3
    public void Rv64a_ErrorCases_ShowUnknown(string hex)
    {
        var r = Disasm(hex);
        r.Should().MatchRegex("(?i)(unknown|illegal)");
    }
}
