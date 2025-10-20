using System.Linq;
using FluentAssertions;
using RiscVAssembler.Assembler;
using RiscVAssembler.RiscV;
using Xunit;

namespace RiscVAssembler.Tests;

public class RvfExtensionTests
{
    private static uint[] Assemble(string asm)
    {
        var a = new UnifiedAssembler();
        return a.Assemble(asm).ToArray();
    }

    [Theory]
    [InlineData("fsgnj.s f1, f2, f3", Fpu.FSGNJ_S, 0b000)]
    [InlineData("fsgnjn.s f4, f5, f6", Fpu.FSGNJ_S, 0b001)]
    [InlineData("fsgnjx.s f7, f8, f9", Fpu.FSGNJ_S, 0b010)]
    [InlineData("fmin.s f10, f11, f12", Fpu.FMINMAX_S, 0b000)]
    [InlineData("fmax.s f13, f14, f15", Fpu.FMINMAX_S, 0b001)]
    public void FpSignMinMax_Encodes(string asm, uint funct7, uint sel)
    {
        var words = Assemble(asm);
        words.Length.Should().Be(1);
        var parts = asm.Split(' ');
        var rd = uint.Parse(parts[1].TrimEnd(',').TrimStart('f'));
        var rs1 = uint.Parse(parts[2].TrimEnd(',').TrimStart('f'));
        var rs2 = uint.Parse(parts[3].TrimStart('f'));
        var expected = InstructionBuilder.BuildFpRType(funct7, sel, rd, rs1, rs2, 0);
        words[0].Should().Be(expected);
    }

    [Theory]
    [InlineData("fcvt.w.s x1, f2", Fpu.FCVT_W_S, false)]
    [InlineData("fcvt.wu.s x3, f4", Fpu.FCVT_W_S, true)]
    [InlineData("fcvt.s.w f5, x6", Fpu.FCVT_S_W, false)]
    public void FpCvts_Encodes(string asm, uint funct7, bool flag)
    {
        var words = Assemble(asm);
        words.Length.Should().Be(1);
        var parts = asm.Split(' ');
        if (parts[0].StartsWith("fcvt.w"))
        {
            var rd = uint.Parse(parts[1].TrimEnd(',').TrimStart('x'));
            var rs1 = uint.Parse(parts[2].TrimStart('f'));
            var rs2 = flag ? 0b00001u : 0u;
            var expected = Opcodes.OP_FP | (rd << 7) | (0u << 12) | (rs1 << 15) | (rs2 << 20) | (funct7 << 25);
            words[0].Should().Be(expected);
        }
        else
        {
            var rd = uint.Parse(parts[1].TrimEnd(',').TrimStart('f'));
            var rs1 = uint.Parse(parts[2].TrimStart('x'));
            var rs2 = 0u;
            var expected = Opcodes.OP_FP | (rd << 7) | (0u << 12) | (rs1 << 15) | (rs2 << 20) | (funct7 << 25);
            words[0].Should().Be(expected);
        }
    }

    [Fact]
    public void Fclass_Encodes()
    {
        var w = Assemble("fclass.s x2, f3");
        w.Length.Should().Be(1);
        var expected = InstructionBuilder.BuildFpRType(Fpu.FCLASS_S, 0, 2u, 3u, 0u, 0u);
        w[0].Should().Be(expected);
    }
}
