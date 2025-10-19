using System;
using FluentAssertions;
using RiscVAssembler.Decoder;
using Xunit;

namespace RiscVAssembler.Tests;

public class Rv64iAndFpAndCsrTests
{
    private static string Disasm(string hex)
    {
        var s = hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? hex[2..] : hex;
        uint v = Convert.ToUInt32(s, 16);
        return new UnifiedDisassembler().Disassemble(v);
    }

    [Theory]
    // ld / sd (funct3=011)
    [InlineData("0000b303", "ld")]   // made-up encoding for form check only
    [InlineData("0000b023", "sd")]   // made-up encoding for form check only
    public void Rv64_LoadStore_Heuristic(string hex, string mnemonic)
    {
        // These encodings are placeholders to exercise the module selection paths.
        Disasm(hex).Should().Contain(mnemonic);
    }

    [Theory]
    [InlineData("00052007", "flw")] // flw f0, 0(x10) (pattern check)
    [InlineData("00052027", "fsw")] // fsw f0, 0(x10)
    public void Rvf_LoadStore(string hex, string mnemonic)
    {
        Disasm(hex).Should().Contain(mnemonic);
    }

    [Theory]
    [InlineData("00000053", "fadd.s")] // OP-FP base opcode, funct7=0 => fadd.s (pattern check)
    public void Rvf_OpFp(string hex, string mnemonic)
    {
        Disasm(hex).Should().Contain(mnemonic);
    }

    [Theory]
    [InlineData("00000073", "ecall")]
    [InlineData("00100073", "ebreak")]
    [InlineData("30525073", "csrrw")] // csrrw x0, 0x305, x4
    [InlineData("30526073", "csrrs")] // csrrs x0, 0x305, x4
    [InlineData("30527073", "csrrc")] // csrrc x0, 0x305, x4
    [InlineData("3052d073", "csrrwi")] // csrrwi x0, 0x305, 4
    [InlineData("3052e073", "csrrsi")] // csrrsi x0, 0x305, 4
    [InlineData("3052f073", "csrrci")] // csrrci x0, 0x305, 4
    public void Rvzicsr_SystemAndCsr(string hex, string mnemonic)
    {
        Disasm(hex).Should().Contain(mnemonic);
    }
}
