using System;
using System.Linq;
using FluentAssertions;
using RiscVAssembler.Assembler;
using RiscVAssembler.Decoder;
using Xunit;

namespace RiscVAssembler.Tests;

public class RoundtripAsmDisasmTests
{
    private static uint AssembleOne(string asm)
    {
        var asmblr = new UnifiedAssembler();
        return asmblr.Assemble(asm).Single();
    }

    private static string Disasm(uint code)
    {
        return new UnifiedDisassembler().Disassemble(code);
    }

    [Theory]
    [InlineData("addi x1, zero, 42", "addi ra, zero, 42")]
    [InlineData("lw x2, 4(sp)", "lw sp, 4(sp)")]
    [InlineData("beq x1, x2, 8", "beq ra, sp, ")]
    [InlineData("and x5, x6, x7", "and t0, t1, t2")]
    public void AssembleAndDisassemble_ContainsMnemonicAndOperands(string asmText, string expectedContains)
    {
        var code = AssembleOne(asmText);
        var text = Disasm(code);
        text.Should().Contain(expectedContains);
    }
}
