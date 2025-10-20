using System.Linq;
using FluentAssertions;
using RiscVAssembler.Assembler;
using RiscVAssembler.RiscV;
using Xunit;

namespace RiscVAssembler.Tests;

public class RvdExtensionTests
{
    private static uint[] Assemble(string asm)
    {
        var a = new UnifiedAssembler();
        return a.Assemble(asm).ToArray();
    }

    [Fact]
    public void FaddD_Encodes()
    {
        var w = Assemble("fadd.d f1, f2, f3");
        w.Length.Should().Be(1);
        var expected = InstructionBuilder.BuildFpRType(Fpu.FADD_D, 0, 1u, 2u, 3u, 0u);
        w[0].Should().Be(expected);
    }

    [Fact]
    public void FldFsd_Encodes()
    {
        var w = Assemble("fld f4, 8(x5)");
        w.Length.Should().Be(1);
        var expected = InstructionBuilder.BuildIType(Opcodes.LOAD_FP, 0b011, 4u, 5u, 8);
        w[0].Should().Be(expected);

        var s = Assemble("fsd f6, 16(x7)");
        s.Length.Should().Be(1);
        var expected2 = InstructionBuilder.BuildSType(Opcodes.STORE_FP, 0b011, 7u, 6u, 16);
        s[0].Should().Be(expected2);
    }

    [Fact]
    public void FconvD_Encodes()
    {
        var w = Assemble("fcvt.w.d x2, f3");
        w.Length.Should().Be(1);
        var expected = Opcodes.OP_FP | (2u << 7) | (0u << 12) | (3u << 15) | (0u << 20) | (Fpu.FCVT_W_D << 25);
        w[0].Should().Be(expected);

        var w2 = Assemble("fcvt.d.w f4, x5");
        w2.Length.Should().Be(1);
        var expected3 = Opcodes.OP_FP | (4u << 7) | (0u << 12) | (5u << 15) | (0u << 20) | (Fpu.FCVT_D_W << 25);
        w2[0].Should().Be(expected3);
    }

    [Fact]
    public void FclassD_Encodes()
    {
        var w = Assemble("fclass.d x1, f2");
        w.Length.Should().Be(1);
        var expected = InstructionBuilder.BuildFpRType(Fpu.FCLASS_D, 0, 1u, 2u, 0u, 0u);
        w[0].Should().Be(expected);
    }
}
