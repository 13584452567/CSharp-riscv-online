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
        // additionally assert mask/match for OP_FP R-type with 31..27=0x00, 26..25=1, 6..2=0x14, 1..0=3
        uint mask = 0u, match = 0u;
        mask |= 0b11111u << 27; match |= (0x00u << 27);
        mask |= 0b11u << 25; match |= (0b01u << 25);
        mask |= 0b11111u << 2; match |= (0x14u << 2);
        mask |= 0b11u << 0; match |= (0b11u << 0);
        (w[0] & mask).Should().Be(match);
    }

    [Fact]
    public void FldFsd_Encodes()
    {
        var w = Assemble("fld f4, 8(x5)");
        w.Length.Should().Be(1);
        var expected = InstructionBuilder.BuildIType(Opcodes.LOAD_FP, 0b011, 4u, 5u, 8);
        w[0].Should().Be(expected);

        // check LOAD_FP mask/match bits for FLD: 14..12=3, 6..2=0x01, 1..0=3
        uint maskL = 0u, matchL = 0u;
        maskL |= 0b111u << 12; matchL |= (0b011u << 12);
        maskL |= 0b11111u << 2; matchL |= (0x01u << 2);
        maskL |= 0b11u << 0; matchL |= (0b11u << 0);
        (w[0] & maskL).Should().Be(matchL);

        var s = Assemble("fsd f6, 16(x7)");
        s.Length.Should().Be(1);
        var expected2 = InstructionBuilder.BuildSType(Opcodes.STORE_FP, 0b011, 7u, 6u, 16);
        s[0].Should().Be(expected2);
        // check STORE_FP mask/match for FSD: 14..12=3, 6..2=0x09, 1..0=3
        uint maskS = 0u, matchS = 0u;
        maskS |= 0b111u << 12; matchS |= (0b011u << 12);
        maskS |= 0b11111u << 2; matchS |= (0x09u << 2);
        maskS |= 0b11u << 0; matchS |= (0b11u << 0);
        (s[0] & maskS).Should().Be(matchS);
    }

    [Fact]
    public void FconvD_Encodes()
    {
        var w = Assemble("fcvt.w.d x2, f3");
        w.Length.Should().Be(1);
        var expected = Opcodes.OP_FP | (2u << 7) | (0u << 12) | (3u << 15) | (0u << 20) | (Fpu.FCVT_W_D << 25);
        w[0].Should().Be(expected);

        // check mask/match for FCVT.W.D: 24..20=0, 31..27=0x18, 26..25=1, 6..2=0x14, 1..0=3
        uint mask = 0u, match = 0u;
        mask |= 0b11111u << 27; match |= (0x18u << 27);
        mask |= 0b11u << 25; match |= (0b01u << 25);
        mask |= 0b11111u << 20; match |= (0x00u << 20);
        mask |= 0b11111u << 2; match |= (0x14u << 2);
        mask |= 0b11u << 0; match |= (0b11u << 0);
        (w[0] & mask).Should().Be(match);

        var w2 = Assemble("fcvt.d.w f4, x5");
        w2.Length.Should().Be(1);
        var expected3 = Opcodes.OP_FP | (4u << 7) | (0u << 12) | (5u << 15) | (0u << 20) | (Fpu.FCVT_D_W << 25);
        w2[0].Should().Be(expected3);
        // check mask/match for FCVT.D.W: 24..20=0, 31..27=0x1A, 26..25=1
        uint mask2 = 0u, match2 = 0u;
        mask2 |= 0b11111u << 27; match2 |= (0x1Au << 27);
        mask2 |= 0b11u << 25; match2 |= (0b01u << 25);
        mask2 |= 0b11111u << 20; match2 |= (0x00u << 20);
        mask2 |= 0b11111u << 2; match2 |= (0x14u << 2);
        mask2 |= 0b11u << 0; match2 |= (0b11u << 0);
        (w2[0] & mask2).Should().Be(match2);
    }

    [Fact]
    public void FclassD_Encodes()
    {
        var w = Assemble("fclass.d x1, f2");
        w.Length.Should().Be(1);
        var expected = InstructionBuilder.BuildFpRType(Fpu.FCLASS_D, 0, 1u, 2u, 0u, 0u);
        w[0].Should().Be(expected);
        // check mask/match for FCLASS.D: 24..20=0, 31..27=0x1C, 14..12=1, 26..25=1
        uint mask = 0u, match = 0u;
        mask |= 0b11111u << 27; match |= (0x1Cu << 27);
        mask |= 0b11u << 25; match |= (0b01u << 25);
        mask |= 0b11111u << 20; match |= (0x00u << 20);
        mask |= 0b111u << 12; match |= (0b001u << 12);
        mask |= 0b11111u << 2; match |= (0x14u << 2);
        mask |= 0b11u << 0; match |= (0b11u << 0);
        (w[0] & mask).Should().Be(match);
    }
}
