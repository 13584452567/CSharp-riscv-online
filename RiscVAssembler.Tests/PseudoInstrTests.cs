using System.Linq;
using System.Collections.Generic;
using FluentAssertions;
using RiscVAssembler.Assembler;
using RiscVAssembler.RiscV;
using Xunit;

namespace RiscVAssembler.Tests;

public class PseudoInstrTests
{
    private static uint[] Assemble(string asm)
    {
        var a = new UnifiedAssembler();
        return a.Assemble(asm).ToArray();
    }

    private static int RegIndex(string token)
    {
        token = token.Trim();
        if (token.StartsWith("x")) return int.Parse(token[1..]);
        return token switch
        {
            "zero" => 0, "ra" => 1, "sp" => 2, "gp" => 3, "tp" => 4,
            "t0" => 5, "t1" => 6, "t2" => 7,
            "s0" or "fp" => 8, "s1" => 9,
            "a0" => 10, "a1" => 11, "a2" => 12, "a3" => 13, "a4" => 14, "a5" => 15,
            "a6" => 16, "a7" => 17,
            "s2" => 18, "s3" => 19, "s4" => 20, "s5" => 21, "s6" => 22, "s7" => 23,
            "s8" => 24, "s9" => 25, "s10" => 26, "s11" => 27,
            "t3" => 28, "t4" => 29, "t5" => 30, "t6" => 31,
            _ => throw new KeyNotFoundException($"Unknown register token: {token}")
        };
    }

    private static (uint[], uint[]) LiExpected(int rd, int imm)
    {
        if (imm >= -2048 && imm <= 2047)
        {
            var w = InstructionBuilder.BuildIType(Opcodes.OP_IMM, 0b000, (uint)rd, 0, imm);
            return (new[] { w }, new uint[0]);
        }
        int imm_lo = imm & 0xFFF;
        if (imm_lo >= 0x800) imm_lo -= 0x1000;
        int imm_hi = imm - imm_lo;
        uint lui = InstructionBuilder.BuildUType(Opcodes.LUI, (uint)rd, imm_hi);
        uint addi = InstructionBuilder.BuildIType(Opcodes.OP_IMM, 0b000, (uint)rd, (uint)rd, imm_lo);
        return (new[] { lui, addi }, new uint[0]);
    }

    [Fact]
    public void Li_SmallImmediate_ExactMatch()
    {
        var words = Assemble("li x5, 123");
        words.Length.Should().Be(1);
        var expected = InstructionBuilder.BuildIType(Opcodes.OP_IMM, 0b000, 5u, 0u, 123);
        words[0].Should().Be(expected);
    }

    [Fact]
    public void Li_LargeImmediate_ExactMatch()
    {
        var words = Assemble("li x6, 0x12345000");
        words.Length.Should().Be(2);
        var (expected, _) = LiExpected(6, (int)0x12345000);
        words[0].Should().Be(expected[0]);
        words[1].Should().Be(expected[1]);
    }

    [Fact]
    public void La_LargeImmediate_ExactMatch()
    {
        var words = Assemble("la x7, 0x12345000");
        words.Length.Should().Be(2);
        var (expected, _) = LiExpected(7, (int)0x12345000);
        words[0].Should().Be(expected[0]);
        words[1].Should().Be(expected[1]);
    }

    [Fact]
    public void PushPop_SingleRegister_Exact()
    {
        var push = Assemble("push t0");
        push.Length.Should().Be(2);
        var exp0 = InstructionBuilder.BuildIType(Opcodes.OP_IMM, 0b000, 2u, 2u, -4);
        var exp1 = InstructionBuilder.BuildSType(Opcodes.STORE, 0b010, 2u, (uint)RegIndex("t0"), 0);
        push[0].Should().Be(exp0);
        push[1].Should().Be(exp1);

        var pop = Assemble("pop t0");
        pop.Length.Should().Be(2);
        var p0 = InstructionBuilder.BuildIType(Opcodes.LOAD, 0b010, (uint)RegIndex("t0"), 2u, 0);
        var p1 = InstructionBuilder.BuildIType(Opcodes.OP_IMM, 0b000, 2u, 2u, 4);
        pop[0].Should().Be(p0);
        pop[1].Should().Be(p1);
    }

    [Fact]
    public void PushmPopm_MultipleRegisters_Exact()
    {
        var pushm = Assemble("pushm t0, t1, t2");
        pushm.Length.Should().Be(4);
        var expAddi = InstructionBuilder.BuildIType(Opcodes.OP_IMM, 0b000, 2u, 2u, -12);
        pushm[0].Should().Be(expAddi);
        pushm[1].Should().Be(InstructionBuilder.BuildSType(Opcodes.STORE, 0b010, 2u, (uint)RegIndex("t0"), 0));
        pushm[2].Should().Be(InstructionBuilder.BuildSType(Opcodes.STORE, 0b010, 2u, (uint)RegIndex("t1"), 4));
        pushm[3].Should().Be(InstructionBuilder.BuildSType(Opcodes.STORE, 0b010, 2u, (uint)RegIndex("t2"), 8));

        var popm = Assemble("popm t0, t1, t2");
        popm.Length.Should().Be(4);
        popm[0].Should().Be(InstructionBuilder.BuildIType(Opcodes.LOAD, 0b010, (uint)RegIndex("t0"), 2u, 0));
        popm[1].Should().Be(InstructionBuilder.BuildIType(Opcodes.LOAD, 0b010, (uint)RegIndex("t1"), 2u, 4));
        popm[2].Should().Be(InstructionBuilder.BuildIType(Opcodes.LOAD, 0b010, (uint)RegIndex("t2"), 2u, 8));
        popm[3].Should().Be(InstructionBuilder.BuildIType(Opcodes.OP_IMM, 0b000, 2u, 2u, 12));
    }

    [Theory]
    [InlineData("bgt x5, x6, 8", 0b100)]
    [InlineData("ble x5, x6, 8", 0b101)]
    [InlineData("bgtu x5, x6, 8", 0b110)]
    [InlineData("bleu x5, x6, 8", 0b111)]
    public void BranchAliases_Exact(string asm, int funct3)
    {
        var words = Assemble(asm);
        words.Length.Should().Be(1);
        // assembler swaps operands: BuildBType(Opcodes.BRANCH, funct3, rs2, rs1, imm)
        var expected = InstructionBuilder.BuildBType(Opcodes.BRANCH, (uint)funct3, (uint)6, (uint)5, 8);
        words[0].Should().Be(expected);
    }

    [Fact]
    public void Call_Exact()
    {
        var w = Assemble("call 16");
        w.Length.Should().Be(1);
        var expected = InstructionBuilder.BuildJType(Opcodes.JAL, 1, 16);
        w[0].Should().Be(expected);
    }

    [Fact]
    public void NotNegSeqz_Exact()
    {
        var notw = Assemble("not x8, x9");
        notw.Length.Should().Be(1);
        var expectNot = InstructionBuilder.BuildIType(Opcodes.OP_IMM, 0b100, 8u, 9u, -1);
        notw[0].Should().Be(expectNot);

        var negw = Assemble("neg x10, x11");
        negw.Length.Should().Be(1);
        var expectNeg = InstructionBuilder.BuildRType(Opcodes.OP, 0b000, 0b0100000, 10u, 0u, 11u);
        negw[0].Should().Be(expectNeg);

        var seqw = Assemble("seqz x12, x13");
        seqw.Length.Should().Be(1);
        var expectSeq = InstructionBuilder.BuildIType(Opcodes.OP_IMM, 0b011, 12u, 13u, 1);
        seqw[0].Should().Be(expectSeq);
    }
}
