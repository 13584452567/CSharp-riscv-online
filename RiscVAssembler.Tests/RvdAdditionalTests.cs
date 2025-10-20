using System.Linq;
using FluentAssertions;
using RiscVAssembler.Assembler;
using RiscVAssembler.RiscV;
using Xunit;

namespace RiscVAssembler.Tests;

public class RvdAdditionalTests
{
    private static uint[] Assemble(string asm)
    {
        var a = new UnifiedAssembler();
        return a.Assemble(asm).ToArray();
    }

    private static void AssertMaskMatch(uint word, uint mask, uint match)
    {
        (word & mask).Should().Be(match);
    }

    [Fact]
    public void FmvX_D_Encodes()
    {
        var w = Assemble("fmv.x.d x1, f2");
        w.Length.Should().Be(1);
    // rv64_d: fmv.x.d 31..27=0x1C, 14..12=0, 26..25=1, 6..2=0x14, 1..0=3
    uint mask = 0;
    uint match = 0;
    // bits 31..27
    mask |= 0b11111u << 27;
    match |= (0x1Cu << 27);
    // bits 26..25
    mask |= 0b11u << 25;
    match |= (0b01u << 25);
    // bits 14..12
    mask |= 0b111u << 12;
    match |= (0u << 12);
    // bits 6..2
    mask |= 0b11111u << 2;
    match |= (0x14u << 2);
    // bits 1..0
    mask |= 0b11u << 0;
    match |= (0b11u << 0);
    AssertMaskMatch(w[0], mask, match);
    }

    [Fact]
    public void FmvD_X_Encodes()
    {
        var w = Assemble("fmv.d.x f3, x4");
        w.Length.Should().Be(1);
    // rv64_d: fmv.d.x 31..27=0x1E, 14..12=0, 26..25=1, 6..2=0x14, 1..0=3
    uint mask2 = 0u;
    uint match2 = 0u;
    mask2 |= 0b11111u << 27; match2 |= (0x1Eu << 27);
    mask2 |= 0b11u << 25; match2 |= (0b01u << 25);
    mask2 |= 0b111u << 12; match2 |= (0u << 12);
    mask2 |= 0b11111u << 2; match2 |= (0x14u << 2);
    mask2 |= 0b11u << 0; match2 |= (0b11u << 0);
    AssertMaskMatch(w[0], mask2, match2);
    }

    [Fact]
    public void FcvtS_D_Encodes()
    {
        var w = Assemble("fcvt.s.d f5, f6");
        w.Length.Should().Be(1);
    // rv_d: fcvt.s.d 24..20=1, 31..27=0x08, 26..25=0, 6..2=0x14, 1..0=3
    uint mask3 = 0u; uint match3 = 0u;
    mask3 |= 0b11111u << 27; match3 |= (0x08u << 27);
    mask3 |= 0b11u << 25; match3 |= (0b00u << 25);
    mask3 |= 0b11111u << 20; match3 |= (0x01u << 20);
    mask3 |= 0b11111u << 2; match3 |= (0x14u << 2);
    mask3 |= 0b11u << 0; match3 |= (0b11u << 0);
    AssertMaskMatch(w[0], mask3, match3);
    }

    [Fact]
    public void FcvtD_S_Encodes()
    {
        var w = Assemble("fcvt.d.s f7, f8");
        w.Length.Should().Be(1);
    // rv_d: fcvt.d.s 24..20=0, 31..27=0x08, 26..25=1, 6..2=0x14, 1..0=3
    uint mask4 = 0u; uint match4 = 0u;
    mask4 |= 0b11111u << 27; match4 |= (0x08u << 27);
    mask4 |= 0b11u << 25; match4 |= (0b01u << 25);
    mask4 |= 0b11111u << 20; match4 |= (0x00u << 20);
    mask4 |= 0b11111u << 2; match4 |= (0x14u << 2);
    mask4 |= 0b11u << 0; match4 |= (0b11u << 0);
    AssertMaskMatch(w[0], mask4, match4);
    }

    [Fact]
    public void Feq_Flt_Fle_Encodes()
    {
        var a = Assemble("feq.d x1, f2, f3");
        a.Length.Should().Be(1);
    // rv_d: feq.d 31..27=0x14, 14..12=2, 26..25=1, 6..2=0x14, 1..0=3
    uint maskEq = 0u; uint matchEq = 0u;
    maskEq |= 0b11111u << 27; matchEq |= (0x14u << 27);
    maskEq |= 0b11u << 25; matchEq |= (0b01u << 25);
    maskEq |= 0b111u << 12; matchEq |= (0b010u << 12);
    maskEq |= 0b11111u << 2; matchEq |= (0x14u << 2);
    maskEq |= 0b11u << 0; matchEq |= (0b11u << 0);
    AssertMaskMatch(a[0], maskEq, matchEq);

        var b = Assemble("flt.d x4, f5, f6");
        b.Length.Should().Be(1);
    // flt.d 14..12=1
    uint maskLt = maskEq; uint matchLt = (matchEq & ~(0b111u << 12)) | (0b001u << 12);
    AssertMaskMatch(b[0], maskLt, matchLt);

        var c = Assemble("fle.d x7, f8, f9");
        c.Length.Should().Be(1);
    // fle.d 14..12=0
    uint maskLe = maskEq; uint matchLe = (matchEq & ~(0b111u << 12)) | (0b000u << 12);
    AssertMaskMatch(c[0], maskLe, matchLe);
    }

    [Fact]
    public void FmaddD_Encodes()
    {
        var w = Assemble("fmadd.d f10, f11, f12, f13");
        w.Length.Should().Be(1);
        var expected = InstructionBuilder.BuildFpR4Type(Opcodes.MADD, 10u, 11u, 12u, 13u, 0u);
        w[0].Should().Be(expected);
    }

    [Fact]
    public void FcvtL_D_and_D_L_Encodes()
    {
        var w = Assemble("fcvt.l.d x2, f3");
        w.Length.Should().Be(1);
        // fcvt.l.d uses rs2 = 0 for signed
        var expected = Opcodes.OP_FP | (2u << 7) | (0u << 12) | (3u << 15) | (0u << 20) | (Fpu.FCVT_L_D << 25);
        w[0].Should().Be(expected);

        var w2 = Assemble("fcvt.lu.d x4, f5");
        w2.Length.Should().Be(1);
        var expected2 = Opcodes.OP_FP | (4u << 7) | (0u << 12) | (5u << 15) | (1u << 20) | (Fpu.FCVT_L_D << 25);
        w2[0].Should().Be(expected2);

        var w3 = Assemble("fcvt.d.l f6, x7");
        w3.Length.Should().Be(1);
        var expected3 = Opcodes.OP_FP | (6u << 7) | (0u << 12) | (7u << 15) | (0u << 20) | (Fpu.FCVT_D_L << 25);
        w3[0].Should().Be(expected3);

        var w4 = Assemble("fcvt.d.lu f8, x9");
        w4.Length.Should().Be(1);
        var expected4 = Opcodes.OP_FP | (8u << 7) | (0u << 12) | (9u << 15) | (1u << 20) | (Fpu.FCVT_D_L << 25);
        w4[0].Should().Be(expected4);
    }
}
