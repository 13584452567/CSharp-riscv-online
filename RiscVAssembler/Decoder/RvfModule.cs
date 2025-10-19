// RiscVAssembler/Decoder/RvfModule.cs
using RiscVAssembler.RiscV;

namespace RiscVAssembler.Decoder;

public class RvfModule : IDisassemblerModule
{
    public bool TryDisassemble(uint instruction, out string text)
    {
        uint opcode = instruction & 0x7F;
        switch (opcode)
        {
            case Opcodes.LOAD_FP:
                text = DecodeLoadFp(instruction); return true;
            case Opcodes.STORE_FP:
                text = DecodeStoreFp(instruction); return true;
            case Opcodes.OP_FP:
                text = DecodeOpFp(instruction); return true;
            default:
                text = string.Empty; return false;
        }
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
}
