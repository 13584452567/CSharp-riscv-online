// RiscVAssembler/Decoder/Rv64iModule.cs
using RiscVAssembler.RiscV;

namespace RiscVAssembler.Decoder;

public class Rv64iModule : IDisassemblerModule
{
    public bool TryDisassemble(uint instruction, out string text)
    {
        uint opcode = instruction & 0x7F;
        switch (opcode)
        {
            case Opcodes.OP_IMM_32:
                text = DecodeITypeOpImmW(instruction); return true;
            case Opcodes.OP_32:
                text = DecodeRTypeW(instruction); return true;
            case Opcodes.LOAD:
                {
                    var funct3 = (instruction >> 12) & 0x7;
                    if (funct3 == 0b011) { text = DecodeLoadStore64(instruction, isLoad:true); return true; }
                    break;
                }
            case Opcodes.STORE:
                {
                    var funct3 = (instruction >> 12) & 0x7;
                    if (funct3 == 0b011) { text = DecodeLoadStore64(instruction, isLoad:false); return true; }
                    break;
                }
        }
        text = string.Empty; return false;
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
            return $"{name} {RegisterUtils.RegName((int)rd)}, {RegisterUtils.RegName((int)rs1)}, {shamt}";
        }
        var imm = (int)(instruction >> 20);
    return $"{name} {RegisterUtils.RegName((int)rd)}, {RegisterUtils.RegName((int)rs1)}, {imm}";
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
    return $"{name} {RegisterUtils.RegName((int)rd)}, {RegisterUtils.RegName((int)rs1)}, {RegisterUtils.RegName((int)rs2)}";
    }

    private string DecodeLoadStore64(uint instruction, bool isLoad)
    {
        if (isLoad)
        {
            var rd = (instruction >> 7) & 0x1F;
            var rs1 = (instruction >> 15) & 0x1F;
            var imm = (int)(instruction >> 20);
            return $"ld {RegisterUtils.RegName((int)rd)}, {imm}({RegisterUtils.RegName((int)rs1)})";
        }
        else
        {
            var rs1 = (instruction >> 15) & 0x1F;
            var rs2 = (instruction >> 20) & 0x1F;
            var imm4_0 = (instruction >> 7) & 0x1F;
            var imm11_5 = (instruction >> 25) & 0x7F;
            int imm = (int)((imm11_5 << 5) | imm4_0);
            if ((imm & 0x800) != 0) imm |= unchecked((int)~0xFFF);
            return $"sd {RegisterUtils.RegName((int)rs2)}, {imm}({RegisterUtils.RegName((int)rs1)})";
        }
    }
}
