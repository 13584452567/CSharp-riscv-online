// RiscVAssembler/RiscV/Instruction.cs
using RiscVAssembler.Decoder;

namespace RiscVAssembler.RiscV;

/// <summary>
/// Represents a parsed RISC-V instruction.
/// </summary>
public class Instruction
{
    public string Mnemonic { get; }
    public string[] Operands { get; }

    public Instruction(string mnemonic, string[] operands)
    {
        Mnemonic = mnemonic.ToLower();
        Operands = operands;
    }

    public static Instruction Parse(string line)
    {
        var parts = line.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            throw new ArgumentException("Instruction line is empty.");
        }

        var mnemonic = parts[0];
        var operands = parts.Skip(1).ToArray();

        return new Instruction(mnemonic, operands);
    }

    public uint AssembleRType(uint opcode, uint funct3, uint funct7)
    {
        var rd = RegisterUtils.Parse(Operands[0]);
        var rs1 = RegisterUtils.Parse(Operands[1]);
        var rs2 = RegisterUtils.Parse(Operands[2]);
        return InstructionBuilder.BuildRType(opcode, funct3, funct7, rd, rs1, rs2);
    }
}
