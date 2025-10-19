using System;
using FluentAssertions;
using RiscVAssembler.Decoder;
using Xunit;

namespace RiscVAssembler.Tests;

public class Rv32aDisasmTests
{
    private static string Disasm(string hex)
    {
        var s = hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? hex[2..] : hex;
        uint v = Convert.ToUInt32(s, 16);
        return new UnifiedDisassembler(Xlen.X32).Disassemble(v);
    }

    [Fact]
    public void Rv32a_LrSc()
    {
        Disasm("100120af").Should().Contain("lr.w");
        Disasm("183120af").Should().Contain("sc.w");
    }

    [Fact]
    public void Rv32a_Amoswap()
    {
        Disasm("083120af").Should().Contain("amoswap.w");
    }

    [Theory]
    [InlineData("003120af", "amoadd.w")]
    [InlineData("203120af", "amoxor.w")]
    [InlineData("603120af", "amoand.w")]
    [InlineData("403120af", "amoor.w")]
    [InlineData("803120af", "amomin.w")]
    [InlineData("a03120af", "amomax.w")]
    [InlineData("c03120af", "amominu.w")]
    [InlineData("e03120af", "amomaxu.w")]
    public void Rv32a_Arithmetic(string hex, string mnemonic)
    {
        Disasm(hex).Should().Contain(mnemonic);
    }

    [Fact]
    public void Rv32a_AcquireRelease_FlagsIgnoredButMnemonicPresent()
    {
        Disasm("140120af").Should().Contain("lr.w");   // aq
        Disasm("1a3120af").Should().Contain("sc.w");   // rl
        Disasm("0e3120af").Should().Contain("amoswap.w"); // aqrl
    }

    [Theory]
    [InlineData("0050aaaf", "amoadd.w")]
    [InlineData("100f20af", "lr.w")]
    public void Rv32a_VariousRegisters(string hex, string mnemonic)
    {
        Disasm(hex).Should().Contain(mnemonic);
    }

    [Theory]
    [InlineData("100120af", "lr.w")]
    [InlineData("183120af", "sc.w")]
    [InlineData("083120af", "amoswap.w")]
    [InlineData("003120af", "amoadd.w")]
    [InlineData("203120af", "amoxor.w")]
    [InlineData("603120af", "amoand.w")]
    [InlineData("403120af", "amoor.w")]
    [InlineData("803120af", "amomin.w")]
    [InlineData("a03120af", "amomax.w")]
    [InlineData("c03120af", "amominu.w")]
    [InlineData("e03120af", "amomaxu.w")]
    public void Rv32a_Comprehensive(string hex, string mnemonic)
    {
        Disasm(hex).Should().Contain(mnemonic);
    }

    [Theory]
    [InlineData("f83120af")] // invalid funct5
    [InlineData("003130af")] // invalid funct3 (not .w/.d)
    public void Rv32a_ErrorCases_ShowUnknown(string hex)
    {
        var r = Disasm(hex);
        r.Should().MatchRegex("(?i)(unknown|illegal)");
    }
}
