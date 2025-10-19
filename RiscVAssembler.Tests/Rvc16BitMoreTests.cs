using System;
using FluentAssertions;
using RiscVAssembler.Decoder;
using Xunit;

namespace RiscVAssembler.Tests;

public class Rvc16BitMoreTests
{
    private static string D(string hex)
    {
        var s = hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? hex[2..] : hex;
        uint v = Convert.ToUInt32(s, 16);
        return new UnifiedDisassembler().Disassemble(v);
    }

    [Fact]
    public void C0_Quadrant()
    {
    D("0x1000").Should().Contain("c.addi ");
        D("0x4398").Should().Contain("c.lw");
        D("0xc398").Should().Contain("c.sw");
    }

    [Fact]
    public void C1_Quadrant()
    {
        D("0x0001").Should().Contain("c.nop");
        D("0x0505").Should().Contain("c.addi");
        D("0x4501").Should().Contain("c.li");
        D("0xa001").Should().Contain("c.j ");
    }

    [Fact]
    public void C2_Quadrant()
    {
        D("0x4082").Should().Contain("c.lwsp");
        D("0xc006").Should().Contain("c.swsp");
        D("0x8082").Should().Contain("c.jr");
        D("0x9002").Should().Contain("c.ebreak");
    }

    [Fact]
    public void Rvc_Invalid()
    {
        var r1 = D("0x0000"); // c.addi4spn with zero imm
        r1.Should().MatchRegex("(?i)(illegal|unknown)");
    }
}
