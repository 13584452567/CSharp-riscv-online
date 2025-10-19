// RiscVAssembler/RiscV/Instruction.cs
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
}
