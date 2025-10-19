// RiscVAssembler/Decoder/RvzicsrModule.cs
using RiscVAssembler.RiscV;

namespace RiscVAssembler.Decoder;

public class RvzicsrModule : IDisassemblerModule
{
    public bool TryDisassemble(uint instruction, out string text)
    {
        if ((instruction & 0x7F) != Opcodes.SYSTEM) { text = string.Empty; return false; }
        var funct3 = (instruction >> 12) & 0x7;
        var rd = (instruction >> 7) & 0x1F;
        var rs1 = (instruction >> 15) & 0x1F;
        var csr = (instruction >> 20) & 0xFFF;
        text = funct3 switch
        {
            0b000 => ((instruction >> 20) & 0xFFF) switch { 0 => "ecall", 1 => "ebreak", _ => "system" },
            0b001 => $"csrrw {RegisterUtils.RegName((int)rd)}, 0x{csr:X3}, {RegisterUtils.RegName((int)rs1)}",
            0b010 => $"csrrs {RegisterUtils.RegName((int)rd)}, 0x{csr:X3}, {RegisterUtils.RegName((int)rs1)}",
            0b011 => $"csrrc {RegisterUtils.RegName((int)rd)}, 0x{csr:X3}, {RegisterUtils.RegName((int)rs1)}",
            0b101 => $"csrrwi {RegisterUtils.RegName((int)rd)}, 0x{csr:X3}, {(rs1 & 0x1F)}",
            0b110 => $"csrrsi {RegisterUtils.RegName((int)rd)}, 0x{csr:X3}, {(rs1 & 0x1F)}",
            0b111 => $"csrrci {RegisterUtils.RegName((int)rd)}, 0x{csr:X3}, {(rs1 & 0x1F)}",
            _ => "system"
        };
        return true;
    }
}
