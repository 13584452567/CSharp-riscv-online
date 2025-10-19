// RiscVAssembler/Decoder/Rv32iModule.cs
using RiscVAssembler.RiscV;

namespace RiscVAssembler.Decoder;

public class Rv32iModule : IDisassemblerModule
{
    public bool TryDisassemble(uint instruction, out string text)
    {
        uint opcode = instruction & 0x7F;
        switch (opcode)
        {
            case Opcodes.LUI:
                text = DecodeUType(instruction, "lui"); return true;
            case Opcodes.AUIPC:
                text = DecodeUType(instruction, "auipc"); return true;
            case Opcodes.JAL:
                text = DecodeJType(instruction); return true;
            case Opcodes.JALR:
                text = DecodeIType(instruction, "jalr"); return true;
            case Opcodes.BRANCH:
                text = DecodeBType(instruction); return true;
            case Opcodes.LOAD:
                {
                    var funct3 = (instruction >> 12) & 0x7;
                    // In RV64, ld uses funct3=011. Let Rv64iModule handle that case.
                    if (funct3 == 0b011) { text = string.Empty; return false; }
                    text = DecodeIType(instruction, GetLoadInstructionName); return true;
                }
            case Opcodes.STORE:
                {
                    var funct3 = (instruction >> 12) & 0x7;
                    // In RV64, sd uses funct3=011. Let Rv64iModule handle that case.
                    if (funct3 == 0b011) { text = string.Empty; return false; }
                    text = DecodeSType(instruction); return true;
                }
            case Opcodes.OP_IMM:
                text = DecodeITypeOpImm(instruction); return true;
            case Opcodes.OP:
                text = DecodeRType(instruction); return true;
            case Opcodes.FENCE:
                text = DecodeFence(instruction); return true;
            default:
                text = string.Empty; return false;
        }
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
            return $"{name} {RegisterUtils.RegName((int)rd)}, {RegisterUtils.RegName((int)rs1)}, {shamt}";
        }
        return $"{name} {RegisterUtils.RegName((int)rd)}, {RegisterUtils.RegName((int)rs1)}, {imm}";
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
    return $"{name} {RegisterUtils.RegName((int)rd)}, {RegisterUtils.RegName((int)rs1)}, {RegisterUtils.RegName((int)rs2)}";
    }

    private string DecodeIType(uint instruction, Func<uint, string> nameResolver)
    {
        var rd = (instruction >> 7) & 0x1F;
        var funct3 = (instruction >> 12) & 0x7;
        var rs1 = (instruction >> 15) & 0x1F;
        var imm = (int)(instruction >> 20);
        var name = nameResolver(funct3);

        if (name == "slli" || name == "srli" || name == "srai")
        {
            var shamt = (instruction >> 20) & 0x1F;
            return $"{name} {RegisterUtils.RegName((int)rd)}, {RegisterUtils.RegName((int)rs1)}, {shamt}";
        }
        if ((instruction & 0x7F) == Opcodes.LOAD)
        {
            return $"{name} {RegisterUtils.RegName((int)rd)}, {imm}({RegisterUtils.RegName((int)rs1)})";
        }
        return $"{name} {RegisterUtils.RegName((int)rd)}, {RegisterUtils.RegName((int)rs1)}, {imm}";
    }

    private string DecodeIType(uint instruction, string name)
    {
        var rd = (instruction >> 7) & 0x1F;
        var rs1 = (instruction >> 15) & 0x1F;
        var imm = (int)(instruction >> 20);
    return $"{name} {RegisterUtils.RegName((int)rd)}, {RegisterUtils.RegName((int)rs1)}, {imm}";
    }

    private string DecodeSType(uint instruction)
    {
        var funct3 = (instruction >> 12) & 0x7;
        var rs1 = (instruction >> 15) & 0x1F;
        var rs2 = (instruction >> 20) & 0x1F;
        var imm4_0 = (instruction >> 7) & 0x1F;
        var imm11_5 = (instruction >> 25) & 0x7F;
        var imm = (int)((imm11_5 << 5) | imm4_0);
        if ((imm & 0x800) != 0) imm |= unchecked((int)~0xFFF);
        var name = GetStoreInstructionName(funct3);
    return $"{name} {RegisterUtils.RegName((int)rs2)}, {imm}({RegisterUtils.RegName((int)rs1)})";
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
        if ((imm & 0x1000) != 0) imm |= unchecked((int)~0x1FFF);
        var name = funct3 switch
        {
            0b000 => "beq", 0b001 => "bne", 0b100 => "blt",
            0b101 => "bge", 0b110 => "bltu", 0b111 => "bgeu",
            _ => "unknown_branch"
        };
    return $"{name} {RegisterUtils.RegName((int)rs1)}, {RegisterUtils.RegName((int)rs2)}, {imm}";
    }

    private string DecodeUType(uint instruction, string name)
    {
        var rd = (instruction >> 7) & 0x1F;
        var imm = instruction & 0xFFFFF000;
    // wasm prints immediates in decimal via {:?}, but U-type is typically hex; keep hex for readability
    return $"{name} {RegisterUtils.RegName((int)rd)}, 0x{imm:X5}";
    }

    private string DecodeJType(uint instruction)
    {
        var rd = (instruction >> 7) & 0x1F;
        var imm20 = (instruction >> 31) & 0x1;
        var imm10_1 = (instruction >> 21) & 0x3FF;
        var imm11 = (instruction >> 20) & 0x1;
        var imm19_12 = (instruction >> 12) & 0xFF;
        int imm = (int)((imm10_1 << 1) | (imm11 << 11) | (imm19_12 << 12) | (imm20 << 20));
        if ((imm & 0x100000) != 0) imm |= unchecked((int)~0x1FFFFF);
    return $"jal {RegisterUtils.RegName((int)rd)}, {imm}";
    }

    private string DecodeFence(uint instruction)
    {
        var funct3 = (instruction >> 12) & 0x7;
        // We keep it simple: identify fence vs fence.i; ignore pred/succ bits for now.
        return funct3 switch
        {
            0b000 => "fence",
            0b001 => "fence.i",
            _ => "unknown"
        };
    }
}
