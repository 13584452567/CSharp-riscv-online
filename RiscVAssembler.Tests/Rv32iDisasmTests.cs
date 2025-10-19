using System;
using FluentAssertions;
using RiscVAssembler.Decoder;
using Xunit;

namespace RiscVAssembler.Tests;

public class Rv32iDisasmTests
{
    private static string Disasm(string hex)
    {
        // Allow inputs like "0x...." or plain hex
        var s = hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? hex[2..] : hex;
        uint v = Convert.ToUInt32(s, 16);
        return new UnifiedDisassembler().Disassemble(v);
    }

    [Theory]
    [InlineData("12345037", "lui")]
    [InlineData("12345117", "auipc")]
    [InlineData("008000ef", "jal")]
    [InlineData("004100e7", "jalr")]
    [InlineData("00208463", "beq")]
    [InlineData("00209463", "bne")]
    [InlineData("0020c463", "blt")]
    [InlineData("0020d463", "bge")]
    [InlineData("0020e463", "bltu")]
    [InlineData("0020f463", "bgeu")]
    public void Rv32i_Basic_Mnemonics(string hex, string mnemonic)
    {
        var result = Disasm(hex);
        result.Should().Contain(mnemonic);
    }

    [Theory]
    [InlineData("00410083", "lb")]
    [InlineData("00411083", "lh")]
    [InlineData("00412083", "lw")]
    [InlineData("00414083", "lbu")]
    [InlineData("00415083", "lhu")]
    public void Rv32i_Loads(string hex, string mnemonic)
    {
        Disasm(hex).Should().Contain(mnemonic);
    }

    [Theory]
    [InlineData("00110223", "sb")]
    [InlineData("00111223", "sh")]
    [InlineData("00112223", "sw")]
    public void Rv32i_Stores(string hex, string mnemonic)
    {
        Disasm(hex).Should().Contain(mnemonic);
    }

    [Theory]
    [InlineData("06410093", "addi")]
    [InlineData("06412093", "slti")]
    [InlineData("06413093", "sltiu")]
    [InlineData("06414093", "xori")]
    [InlineData("06416093", "ori")]
    [InlineData("06417093", "andi")]
    public void Rv32i_OpImm(string hex, string mnemonic)
    {
        Disasm(hex).Should().Contain(mnemonic);
    }

    [Theory]
    [InlineData("00511093", "slli")]
    [InlineData("00515093", "srli")]
    [InlineData("40515093", "srai")]
    public void Rv32i_ShiftImm(string hex, string mnemonic)
    {
        Disasm(hex).Should().Contain(mnemonic);
    }

    [Theory]
    [InlineData("003100b3", "add")]
    [InlineData("403100b3", "sub")]
    [InlineData("003120b3", "slt")]
    [InlineData("003130b3", "sltu")]
    [InlineData("003140b3", "xor")]
    [InlineData("003150b3", "srl")]
    [InlineData("403150b3", "sra")]
    [InlineData("003160b3", "or")]
    [InlineData("003170b3", "and")]
    public void Rv32i_RType(string hex, string mnemonic)
    {
        Disasm(hex).Should().Contain(mnemonic);
    }

    [Theory]
    [InlineData("00000073", "ecall")]
    [InlineData("00100073", "ebreak")]
    [InlineData("0000000f", "fence")]
    [InlineData("0000100f", "fence.i")]
    public void Rv32i_System(string hex, string mnemonic)
    {
        Disasm(hex).Should().Contain(mnemonic);
    }
}
