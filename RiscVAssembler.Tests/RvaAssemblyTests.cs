using System.Linq;
using FluentAssertions;
using RiscVAssembler.Assembler;
using RiscVAssembler.RiscV;
using Xunit;

namespace RiscVAssembler.Tests;

public class RvaAssemblyTests
{
    private static uint[] Assemble(string asm)
    {
        var a = new UnifiedAssembler();
        return a.Assemble(asm).ToArray();
    }

    [Fact]
    public void LrD_Aq_Rl_Flags()
    {
        var w = Assemble("lr.d.aq x1, (x2)");
        w.Length.Should().Be(1);
        // funct5=0b00010, aq=1 -> bit5 set, rl=0
        uint expectedFunct7 = (0b00010u & 0x1Fu) | (1u << 5) | (0u << 6);
        var expected = InstructionBuilder.BuildAmo(expectedFunct7, false, false, 0b011, 1u, 2u, 0u);
        // Note: BuildAmo expects funct5 and manages aq/rl; to compare raw word we recreate via BuildAmo overload
        var raw = InstructionBuilder.BuildAmo(0b00010, true, false, 0b011, 1u, 2u, 0u);
        w[0].Should().Be(raw);
    }

    [Fact]
    public void Amoswap_Aqrl()
    {
        var w = Assemble("amoswap.d.aqrl x5, x6, x7");
        w.Length.Should().Be(1);
        var raw = InstructionBuilder.BuildAmo(0b00001, true, true, 0b011, 5u, 6u, 7u);
        w[0].Should().Be(raw);
    }

    [Fact]
    public void AmoaddW_Encodes()
    {
        var w = Assemble("amoadd.w x1, x2, x3");
        w.Length.Should().Be(1);
        var raw = InstructionBuilder.BuildAmo(0b00000, false, false, 0b010, 1u, 2u, 3u);
        w[0].Should().Be(raw);
    }
}
