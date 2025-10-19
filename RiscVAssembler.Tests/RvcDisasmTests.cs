using System;
using FluentAssertions;
using RiscVAssembler.Decoder;
using Xunit;

namespace RiscVAssembler.Tests;

public class RvcDisasmTests
{
    private static string Disasm16(string hex)
    {
        var s = hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? hex[2..] : hex;
        uint v = Convert.ToUInt32(s, 16);
        return new UnifiedDisassembler().Disassemble(v);
    }

    [Theory]
    [InlineData("0x1000", "c.addi ")] // wasm prints as c.addi rd', sp, imm
    [InlineData("0x4398", "c.lw")]
    [InlineData("0xc398", "c.sw")]
    [InlineData("0x0001", "c.nop")]
    [InlineData("0x0505", "c.addi")]
    [InlineData("0x4501", "c.li")]
    [InlineData("0xa001", "c.j ")]
    [InlineData("0x4082", "c.lwsp")]
    [InlineData("0xc006", "c.swsp")]
    [InlineData("0x8082", "c.jr")]
    [InlineData("0x9002", "c.ebreak")]
    public void Rvc_Known(string hex, string shouldContain)
    {
        Disasm16(hex).Should().Contain(shouldContain);
    }

    [Fact]
    public void Rvc_Illegal_Addi4spn_Zero()
    {
        Disasm16("0x0000").Should().Be("illegal");
    }

    [Fact]
    public void Rvc_CJAL_Example_3361()
    {
        // 0x00003361 should decode as c.jal zero,832 per wasm core logic
        Disasm16("0x3361").Should().Be("c.jal zero,832");
    }
}
