// RiscVAssembler/RiscV/Opcodes.cs
namespace RiscVAssembler.RiscV;

/// <summary>
/// Defines constants for RISC-V opcodes, funct3, and funct7 values.
/// </summary>
public static class Opcodes
{
    // Opcodes
    public const uint LUI = 0b0110111;
    public const uint AUIPC = 0b0010111;
    public const uint JAL = 0b1101111;
    public const uint JALR = 0b1100111;
    public const uint BRANCH = 0b1100011;
    public const uint LOAD = 0b0000011;
    public const uint STORE = 0b0100011;
    public const uint OP_IMM = 0b0010011;
    public const uint OP = 0b0110011;
    public const uint FENCE = 0b0001111;
    public const uint SYSTEM = 0b1110011;
    // RV64I additional opcodes
    public const uint OP_IMM_32 = 0b0011011; // addiw/slliw/srliw/sraiw
    public const uint OP_32 = 0b0111011;     // addw/subw/sllw/srlw/sraw

    // Atomic instructions (A extension)
    public const uint AMO = 0b0101111;

    // Floating point
    public const uint LOAD_FP = 0b0000111;
    public const uint STORE_FP = 0b0100111;
    public const uint OP_FP = 0b1010011;
    public const uint MADD = 0b1000011;
    public const uint MSUB = 0b1000111;
    public const uint NMSUB = 0b1001011;
    public const uint NMADD = 0b1001111;

    public static bool IsLoadInstruction(uint opcode) => opcode == LOAD;
}
