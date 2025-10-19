using System;
using FluentAssertions;
using RiscVAssembler.Decoder;
using Xunit;

namespace RiscVAssembler.Tests;

public class CrossParityWithWasmTests
{
    private static string Disasm(string hex, Xlen xlen = Xlen.X32)
    {
        var s = hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? hex[2..] : hex;
        uint v = Convert.ToUInt32(s, 16);
        return new UnifiedDisassembler(xlen).Disassemble(v);
    }

    [Theory]
    // RVC: c.jal zero,832
    [InlineData("0x00003361", "c.jal zero,832")]
    // RVC: c.lwsp ra, 0(sp)
    [InlineData("0x00004082", "c.lwsp ra, 0(sp)")]
    // RVC: c.swsp ra, 0(sp)
    [InlineData("0x0000c006", "c.swsp ra, 0(sp)")]
    // RVC: c.jr zero, 0(ra)
    [InlineData("0x00008082", "c.jr zero, 0(ra)")]
    // RVC: c.ebreak
    [InlineData("0x00009002", "c.ebreak")]
    // RVC: c.li a0, 0
    [InlineData("0x00004501", "c.li a0, 0")]
    // RVC: c.addi a0, a0, 1
    [InlineData("0x00000505", "c.addi a0, a0, 1")]
    // RVC: c.lw a4, 0(a5)
    [InlineData("0x00004398", "c.lw a4, 0(a5)")]
    // RVC: c.sw a4, 0(a5)
    [InlineData("0x0000c398", "c.sw a4, 0(a5)")]
    // RVC: c.nop
    [InlineData("0x00000001", "c.nop")]
    // RVC: c.j zero, 0
    [InlineData("0x0000a001", "c.j zero, 0")]
    // RVC: c.addi4spn -> our to_string style prints as c.addi s0, sp, 32
    [InlineData("0x00001000", "c.addi s0, sp, 32")]
    public void Disasm_Should_Match_Wasm_String_Exactly(string hex, string expected)
    {
        Disasm(hex).Should().Be(expected);
    }

    // CSR exact-match samples can be added later once a canonical wasm string set is defined
}
