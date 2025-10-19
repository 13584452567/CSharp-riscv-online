// RiscVAssembler/RiscV/InstructionBuilder.cs
namespace RiscVAssembler.RiscV;

/// <summary>
/// Provides helper methods to build different RISC-V instruction formats.
/// </summary>
public static class InstructionBuilder
{
    /// <summary>
    /// Builds an R-type instruction.
    /// </summary>
    public static uint BuildRType(uint opcode, uint funct3, uint funct7, uint rd, uint rs1, uint rs2)
    {
        return opcode | (rd << 7) | (funct3 << 12) | (rs1 << 15) | (rs2 << 20) | (funct7 << 25);
    }

    /// <summary>
    /// Builds an I-type instruction.
    /// </summary>
    public static uint BuildIType(uint opcode, uint funct3, uint rd, uint rs1, int imm)
    {
        return opcode | (rd << 7) | (funct3 << 12) | (rs1 << 15) | ((uint)imm << 20);
    }

    /// <summary>
    /// Builds an S-type instruction.
    /// </summary>
    public static uint BuildSType(uint opcode, uint funct3, uint rs1, uint rs2, int imm)
    {
        uint imm_4_0 = (uint)imm & 0x1F;
        uint imm_11_5 = ((uint)imm >> 5) & 0x7F;
        return opcode | (imm_4_0 << 7) | (funct3 << 12) | (rs1 << 15) | (rs2 << 20) | (imm_11_5 << 25);
    }

    /// <summary>
    /// Builds a B-type instruction.
    /// </summary>
    public static uint BuildBType(uint opcode, uint funct3, uint rs1, uint rs2, int imm)
    {
        uint imm_11 = ((uint)imm >> 11) & 0x1;
        uint imm_4_1 = ((uint)imm >> 1) & 0xF;
        uint imm_10_5 = ((uint)imm >> 5) & 0x3F;
        uint imm_12 = ((uint)imm >> 12) & 0x1;

        return opcode |
               (imm_11 << 7) |
               (imm_4_1 << 8) |
               (funct3 << 12) |
               (rs1 << 15) |
               (rs2 << 20) |
               (imm_10_5 << 25) |
               (imm_12 << 31);
    }

    /// <summary>
    /// Builds a U-type instruction.
    /// </summary>
    public static uint BuildUType(uint opcode, uint rd, int imm)
    {
        return opcode | (rd << 7) | ((uint)imm & 0xFFFFF000);
    }

    /// <summary>
    /// Builds a J-type instruction.
    /// </summary>
    public static uint BuildJType(uint opcode, uint rd, int imm)
    {
        uint imm_20 = ((uint)imm >> 20) & 0x1;
        uint imm_10_1 = ((uint)imm >> 1) & 0x3FF;
        uint imm_11 = ((uint)imm >> 11) & 0x1;
        uint imm_19_12 = ((uint)imm >> 12) & 0xFF;

        return opcode |
               (rd << 7) |
               (imm_19_12 << 12) |
               (imm_11 << 20) |
               (imm_10_1 << 21) |
               (imm_20 << 31);
    }

    /// <summary>
    /// Builds an AMO (Atomic) instruction (A extension), which is R-type but uses funct5 (bits 31..27), aq (26) and rl (25).
    /// </summary>
    public static uint BuildAmo(uint funct5, bool aq, bool rl, uint funct3, uint rd, uint rs1, uint rs2)
    {
        uint funct7 = (funct5 & 0x1F) | ((aq ? 1u : 0u) << 5) | ((rl ? 1u : 0u) << 6);
        return BuildRType(Opcodes.AMO, funct3, funct7, rd, rs1, rs2);
    }

    /// <summary>
    /// Builds an OP_FP R-type instruction (3 operands f/d registers), using funct7 for operation and funct3 for variants.
    /// </summary>
    public static uint BuildFpRType(uint funct7, uint funct3, uint rd, uint rs1, uint rs2, uint rm)
    {
        // In OP_FP, rm occupies bits 14..12 (funct3 field), and funct7 is bits 31..25
        return Opcodes.OP_FP | (rd << 7) | (rm << 12) | (rs1 << 15) | (rs2 << 20) | (funct7 << 25);
    }

    /// <summary>
    /// Builds an FP R4-type instruction: rd, rs1, rs2, rs3, with rm in bits 14..12.
    /// Opcode varies among MADD/MSUB/NMSUB/NMADD.
    /// </summary>
    public static uint BuildFpR4Type(uint opcode, uint rd, uint rs1, uint rs2, uint rs3, uint rm)
    {
        return opcode | (rd << 7) | (rm << 12) | (rs1 << 15) | (rs2 << 20) | (rs3 << 27);
    }
}
