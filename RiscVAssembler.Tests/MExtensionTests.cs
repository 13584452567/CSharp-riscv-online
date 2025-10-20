using FluentAssertions;
using System.Linq;
using RiscVAssembler.Assembler;
using RiscVAssembler.RiscV;
using Xunit;

namespace RiscVAssembler.Tests;

public class MExtensionTests
{
    private static uint[] Assemble(string asm)
    {
        var a = new UnifiedAssembler();
        return a.Assemble(asm).ToArray();
    }

    [Theory]
    [InlineData("mul x1, x2, x3", 0b000, 0b0000001)]
    [InlineData("mulh x4, x5, x6", 0b001, 0b0000001)]
    [InlineData("div x7, x8, x9", 0b100, 0b0000001)]
    [InlineData("remu x10, x11, x12", 0b111, 0b0000001)]
    public void MInstructions_EncodeCorrectly(string asm, uint funct3, uint funct7)
    {
        var words = Assemble(asm);
        words.Length.Should().Be(1);
        var ins = asm.Split(' ');
        var rd = uint.Parse(ins[1].TrimEnd(',').TrimStart('x'));
        var rs1 = uint.Parse(ins[2].TrimEnd(',').TrimStart('x'));
        var rs2 = uint.Parse(ins[3].TrimStart('x'));
        var expected = InstructionBuilder.BuildRType(Opcodes.OP, funct3, funct7, rd, rs1, rs2);
        words[0].Should().Be(expected);
    }
}
