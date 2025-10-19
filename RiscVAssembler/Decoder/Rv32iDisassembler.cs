// RiscVAssembler/Decoder/Rv32iDisassembler.cs
using RiscVAssembler.RiscV;

namespace RiscVAssembler.Decoder;

/// <summary>
/// Implements the disassembler logic for the RV32I instruction set.
/// </summary>
public class Rv32iDisassembler
{
    public string Disassemble(uint instruction)
    {
        uint opcode = instruction & 0x7F;

        return opcode switch
        {
            Opcodes.LUI    => DecodeUType(instruction, "lui"),
            Opcodes.AUIPC  => DecodeUType(instruction, "auipc"),
            Opcodes.JAL    => DecodeJType(instruction),
            Opcodes.JALR   => DecodeIType(instruction, "jalr"),
            Opcodes.BRANCH => DecodeBType(instruction),
            Opcodes.LOAD   => DecodeIType(instruction, GetLoadInstructionName),
            Opcodes.STORE  => DecodeSType(instruction),
            Opcodes.OP_IMM => DecodeITypeOpImm(instruction),
            Opcodes.OP     => DecodeRType(instruction),
            // RV64I
            Opcodes.OP_IMM_32 => DecodeITypeOpImmW(instruction),
            Opcodes.OP_32     => DecodeRTypeW(instruction),
            // FP
            Opcodes.LOAD_FP => DecodeLoadFp(instruction),
            Opcodes.STORE_FP => DecodeStoreFp(instruction),
            Opcodes.OP_FP => DecodeOpFp(instruction),
            // AMO
            Opcodes.AMO => DecodeAmo(instruction),
            // SYSTEM/CSR
            Opcodes.SYSTEM => DecodeSystem(instruction),
            _              => "unknown"
        };
    }

    private string GetLoadInstructionName(uint funct3) => funct3 switch
    {
        0b000 => "lb", 0b001 => "lh", 0b010 => "lw", 0b100 => "lbu", 0b101 => "lhu",
        _ => "unknown_load"
    };

    private string GetStoreInstructionName(uint funct3) => funct3 switch
    {
        0b000 => "sb", 0b001 => "sh", 0b010 => "sw",
        _ => "unknown_store"
    };

    private string DecodeITypeOpImm(uint instruction)
    {
        var rd = (instruction >> 7) & 0x1F;
        var funct3 = (instruction >> 12) & 0x7;
        var rs1 = (instruction >> 15) & 0x1F;
        var imm = (int)(instruction >> 20);
        string name = funct3 switch
        {
            0b000 => "addi",
            0b010 => "slti",
            0b011 => "sltiu",
            0b100 => "xori",
            0b110 => "ori",
            0b111 => "andi",
            0b001 => "slli",
            0b101 => ((instruction >> 30) & 0x1) == 1 ? "srai" : "srli",
            _ => "unknown"
        };
        if (name is "slli" or "srli" or "srai")
        {
            var shamt = (instruction >> 20) & 0x1F;
            return $"{name} x{rd}, x{rs1}, {shamt}";
        }
        return $"{name} x{rd}, x{rs1}, {imm}";
    }

    private string GetOpInstructionName(uint funct3, uint funct7)
    {
        if (funct3 == 0b000) return funct7 == 0b0100000 ? "sub" : "add";
        if (funct3 == 0b101) return funct7 == 0b0100000 ? "sra" : "srl";

        return funct3 switch
        {
            0b001 => "sll", 0b010 => "slt", 0b011 => "sltu", 0b100 => "xor",
            0b110 => "or", 0b111 => "and",
            _ => "unknown_op"
        };
    }

    private string DecodeRType(uint instruction)
    {
        var rd = (instruction >> 7) & 0x1F;
        var funct3 = (instruction >> 12) & 0x7;
        var rs1 = (instruction >> 15) & 0x1F;
        var rs2 = (instruction >> 20) & 0x1F;
        var funct7 = (instruction >> 25) & 0x7F;
        var name = GetOpInstructionName(funct3, funct7);
        return $"{name} x{rd}, x{rs1}, x{rs2}";
    }

    private string DecodeRTypeW(uint instruction)
    {
        var rd = (instruction >> 7) & 0x1F;
        var funct3 = (instruction >> 12) & 0x7;
        var rs1 = (instruction >> 15) & 0x1F;
        var rs2 = (instruction >> 20) & 0x1F;
        var funct7 = (instruction >> 25) & 0x7F;
        string name = funct3 switch
        {
            0b000 => ((funct7 & 0x40) != 0) ? "subw" : "addw",
            0b001 => "sllw",
            0b101 => ((funct7 & 0x40) != 0) ? "sraw" : "srlw",
            _ => "unknown"
        };
        return $"{name} x{rd}, x{rs1}, x{rs2}";
    }

    private string DecodeIType(uint instruction, Func<uint, string> nameResolver)
    {
        var rd = (instruction >> 7) & 0x1F;
        var funct3 = (instruction >> 12) & 0x7;
        var rs1 = (instruction >> 15) & 0x1F;
        var imm = (int)(instruction >> 20); // Sign-extended
        var name = nameResolver(funct3);

        if (name == "slli" || name == "srli" || name == "srai")
        {
            var shamt = (instruction >> 20) & 0x1F;
            return $"{name} x{rd}, x{rs1}, {shamt}";
        }
        
        if (Opcodes.IsLoadInstruction(instruction & 0x7F))
        {
             return $"{name} x{rd}, {imm}(x{rs1})";
        }

        return $"{name} x{rd}, x{rs1}, {imm}";
    }

    private string DecodeITypeOpImmW(uint instruction)
    {
        var rd = (instruction >> 7) & 0x1F;
        var funct3 = (instruction >> 12) & 0x7;
        var rs1 = (instruction >> 15) & 0x1F;
        string name = funct3 switch
        {
            0b000 => "addiw",
            0b001 => "slliw",
            0b101 => ((instruction >> 30) & 0x1) == 1 ? "sraiw" : "srliw",
            _ => "unknown"
        };
        if (name is "slliw" or "srliw" or "sraiw")
        {
            var shamt = (instruction >> 20) & 0x1F;
            return $"{name} x{rd}, x{rs1}, {shamt}";
        }
        var imm = (int)(instruction >> 20);
        return $"{name} x{rd}, x{rs1}, {imm}";
    }

    private string DecodeIType(uint instruction, string name)
    {
        var rd = (instruction >> 7) & 0x1F;
        var rs1 = (instruction >> 15) & 0x1F;
        var imm = (int)(instruction >> 20); // Sign-extended
        return $"{name} x{rd}, x{rs1}, {imm}";
    }

    private string DecodeSType(uint instruction)
    {
        var funct3 = (instruction >> 12) & 0x7;
        var rs1 = (instruction >> 15) & 0x1F;
        var rs2 = (instruction >> 20) & 0x1F;
        var imm4_0 = (instruction >> 7) & 0x1F;
        var imm11_5 = (instruction >> 25) & 0x7F;
        var imm = (int)((imm11_5 << 5) | imm4_0);
    if ((imm & 0x800) != 0) imm |= unchecked((int)~0xFFF); // Sign extend
        var name = GetStoreInstructionName(funct3);
        return $"{name} x{rs2}, {imm}(x{rs1})";
    }

    private string DecodeBType(uint instruction)
    {
        var funct3 = (instruction >> 12) & 0x7;
        var rs1 = (instruction >> 15) & 0x1F;
        var rs2 = (instruction >> 20) & 0x1F;

        var imm11 = (instruction >> 7) & 0x1;
        var imm4_1 = (instruction >> 8) & 0xF;
        var imm10_5 = (instruction >> 25) & 0x3F;
        var imm12 = (instruction >> 31) & 0x1;

        int imm = (int)((imm4_1 << 1) | (imm10_5 << 5) | (imm11 << 11) | (imm12 << 12));
        if ((imm & 0x1000) != 0) imm |= unchecked((int)~0x1FFF); // Sign extend

        var name = funct3 switch
        {
            0b000 => "beq", 0b001 => "bne", 0b100 => "blt",
            0b101 => "bge", 0b110 => "bltu", 0b111 => "bgeu",
            _ => "unknown_branch"
        };
        return $"{name} x{rs1}, x{rs2}, {imm}"; // Typically shown as offset
    }

    private string DecodeUType(uint instruction, string name)
    {
        var rd = (instruction >> 7) & 0x1F;
        var imm = instruction & 0xFFFFF000;
        return $"{name} x{rd}, 0x{imm:X5}"; // imm is shifted left by 12, so we show the upper 20 bits
    }

    private string DecodeJType(uint instruction)
    {
        var rd = (instruction >> 7) & 0x1F;
        var imm20 = (instruction >> 31) & 0x1;
        var imm10_1 = (instruction >> 21) & 0x3FF;
        var imm11 = (instruction >> 20) & 0x1;
        var imm19_12 = (instruction >> 12) & 0xFF;

        int imm = (int)((imm10_1 << 1) | (imm11 << 11) | (imm19_12 << 12) | (imm20 << 20));
        if ((imm & 0x100000) != 0) imm |= unchecked((int)~0x1FFFFF); // Sign extend

        return $"jal x{rd}, {imm}"; // Typically shown as offset
    }

    private string DecodeLoadFp(uint instruction)
    {
        var rd = (instruction >> 7) & 0x1F;
        var funct3 = (instruction >> 12) & 0x7;
        var rs1 = (instruction >> 15) & 0x1F;
        var imm = (int)(instruction >> 20);
        string name = funct3 switch { 0b010 => "flw", _ => "unknown_fp_load" };
        return $"{name} f{rd}, {imm}(x{rs1})";
    }

    private string DecodeStoreFp(uint instruction)
    {
        var funct3 = (instruction >> 12) & 0x7;
        var rs1 = (instruction >> 15) & 0x1F;
        var rs2 = (instruction >> 20) & 0x1F;
        var imm4_0 = (instruction >> 7) & 0x1F;
        var imm11_5 = (instruction >> 25) & 0x7F;
    var imm = (int)((imm11_5 << 5) | imm4_0);
    if ((imm & 0x800) != 0) imm |= unchecked((int)~0xFFF);
        string name = funct3 switch { 0b010 => "fsw", _ => "unknown_fp_store" };
        return $"{name} f{rs2}, {imm}(x{rs1})";
    }

    private string DecodeOpFp(uint instruction)
    {
        var rd = (instruction >> 7) & 0x1F;
        var rm = (instruction >> 12) & 0x7;
        var rs1 = (instruction >> 15) & 0x1F;
        var rs2 = (instruction >> 20) & 0x1F;
        var funct7 = (instruction >> 25) & 0x7F;
        string name = funct7 switch
        {
            0b0000000 => "fadd.s",
            0b0000100 => "fsub.s",
            0b0001000 => "fmul.s",
            0b0001100 => "fdiv.s",
            0b0101100 => "fsqrt.s",
            _ => "unknown_fp"
        };
        if (name == "fsqrt.s") return $"{name} f{rd}, f{rs1}";
        return $"{name} f{rd}, f{rs1}, f{rs2}";
    }

    private string DecodeAmo(uint instruction)
    {
        var rd = (instruction >> 7) & 0x1F;
        var funct3 = (instruction >> 12) & 0x7;
        var rs1 = (instruction >> 15) & 0x1F;
        var rs2 = (instruction >> 20) & 0x1F;
        var funct7 = (instruction >> 25) & 0x7F;
        var funct5 = funct7 & 0x1F;
        string width = funct3 == 0b010 ? ".w" : funct3 == 0b011 ? ".d" : "";
        string baseName = funct5 switch
        {
            0b00001 => "amoswap", 0b00000 => "amoadd", 0b00100 => "amoxor",
            0b01100 => "amoand", 0b01000 => "amoor",  0b10000 => "amomin",
            0b10100 => "amomax", 0b11000 => "amominu", 0b11100 => "amomaxu",
            0b00010 => (rs2==0?"lr":"unknown"), 0b00011 => "sc",
            _ => "unknown"
        };
        if (baseName == "lr") return $"lr{width} x{rd}, (x{rs1})";
        string name = baseName + width;
        return $"{name} x{rd}, x{rs1}, x{rs2}";
    }

    private string DecodeSystem(uint instruction)
    {
        var funct3 = (instruction >> 12) & 0x7;
        var rd = (instruction >> 7) & 0x1F;
        var rs1 = (instruction >> 15) & 0x1F;
        var csr = (instruction >> 20) & 0xFFF;
        return funct3 switch
        {
            0b000 => ((instruction >> 20) & 0xFFF) switch { 0 => "ecall", 1 => "ebreak", _ => "system" },
            0b001 => $"csrrw x{rd}, 0x{csr:X3}, x{rs1}",
            0b010 => $"csrrs x{rd}, 0x{csr:X3}, x{rs1}",
            0b011 => $"csrrc x{rd}, 0x{csr:X3}, x{rs1}",
            0b101 => $"csrrwi x{rd}, 0x{csr:X3}, {(rs1 & 0x1F)}",
            0b110 => $"csrrsi x{rd}, 0x{csr:X3}, {(rs1 & 0x1F)}",
            0b111 => $"csrrci x{rd}, 0x{csr:X3}, {(rs1 & 0x1F)}",
            _ => "system"
        };
    }
}
