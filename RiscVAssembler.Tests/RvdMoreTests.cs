using System.Linq;
using FluentAssertions;
using RiscVAssembler.Assembler;
using RiscVAssembler.RiscV;
using Xunit;

namespace RiscVAssembler.Tests;

public class RvdMoreTests
{
    private static uint[] Assemble(string asm)
    {
        var a = new UnifiedAssembler();
        return a.Assemble(asm).ToArray();
    }

    [Fact]
    public void FmvD_W_Encodes()
    {
        // fmv.w.d f1, x2  (this is actually the other direction in many assemblers; test basic encode path)
        var w = Assemble("fmv.w.d f1, x2");
        w.Length.Should().Be(1);
        // fmv.w.d is an OP_FP instruction (fmv.w.x / fmv.w.d family uses funct7 from rv64_d: 31..27=0x1E for fmv.w.x?)
        // Use InstructionBuilder.BuildFpRType with FMV_W_X / FMV_X_D mapping: in rv64_d, fmv.w.x uses 31..27=0x1E (fmv.d.x uses 0x1E)
        // rv64_d: fmv.w.d maps to fmv.x.d/fmv.d.x families; use mask/match from rv64_d: 31..27=0x1C, 26..25=1, 14..12=0, 6..2=0x14, 1..0=3
        uint mask = 0u, match = 0u;
        mask |= 0b11111u << 27; match |= (0x1Cu << 27);
        mask |= 0b11u << 25; match |= (0b01u << 25);
        mask |= 0b111u << 12; match |= (0u << 12);
        mask |= 0b11111u << 2; match |= (0x14u << 2);
        mask |= 0b11u << 0; match |= (0b11u << 0);
        (w[0] & mask).Should().Be(match);
    }

    [Fact]
    public void FcvtL_S_Encodes()
    {
        var w = Assemble("fcvt.l.s x3, f4");
        w.Length.Should().Be(1);
        // rv64_d/rv64_f: fcvt.l.s has 24..20=2, 31..27=0x18, 26..25=1, 6..2=0x14, 1..0=3
        uint mask = 0u, match = 0u;
        mask |= 0b11111u << 27; match |= (0x18u << 27);
        mask |= 0b11u << 25; match |= (0b01u << 25);
        mask |= 0b11111u << 20; match |= (0x02u << 20);
        mask |= 0b11111u << 2; match |= (0x14u << 2);
        mask |= 0b11u << 0; match |= (0b11u << 0);
        (w[0] & mask).Should().Be(match);
    }

    [Fact]
    public void FcvtS_L_Encodes()
    {
        var w = Assemble("fcvt.s.l f5, x6");
        w.Length.Should().Be(1);
        // rv64_f: fcvt.s.l 24..20=2, 31..27=0x1A, 26..25=1, 6..2=0x14, 1..0=3
        uint mask = 0u, match = 0u;
        mask |= 0b11111u << 27; match |= (0x1Au << 27);
        mask |= 0b11u << 25; match |= (0b01u << 25);
        mask |= 0b11111u << 20; match |= (0x02u << 20);
        mask |= 0b11111u << 2; match |= (0x14u << 2);
        mask |= 0b11u << 0; match |= (0b11u << 0);
        (w[0] & mask).Should().Be(match);
    }

    [Fact]
    public void FcvtLu_S_Encodes()
    {
        var w = Assemble("fcvt.lu.s x7, f8");
        w.Length.Should().Be(1);
        // rv64_f: fcvt.lu.s 24..20=3, 31..27=0x18, 26..25=1
        uint mask = 0u, match = 0u;
        mask |= 0b11111u << 27; match |= (0x18u << 27);
        mask |= 0b11u << 25; match |= (0b01u << 25);
        mask |= 0b11111u << 20; match |= (0x03u << 20);
        mask |= 0b11111u << 2; match |= (0x14u << 2);
        mask |= 0b11u << 0; match |= (0b11u << 0);
        (w[0] & mask).Should().Be(match);
    }

    [Fact]
    public void FcvtS_Lu_Encodes()
    {
        var w = Assemble("fcvt.s.lu f9, x10");
        w.Length.Should().Be(1);
        // rv64_f: fcvt.s.lu 24..20=3, 31..27=0x1A, 26..25=1
        uint mask = 0u, match = 0u;
        mask |= 0b11111u << 27; match |= (0x1Au << 27);
        mask |= 0b11u << 25; match |= (0b01u << 25);
        mask |= 0b11111u << 20; match |= (0x03u << 20);
        mask |= 0b11111u << 2; match |= (0x14u << 2);
        mask |= 0b11u << 0; match |= (0b11u << 0);
        (w[0] & mask).Should().Be(match);
    }
}
