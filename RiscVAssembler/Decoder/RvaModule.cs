// RiscVAssembler/Decoder/RvaModule.cs
using RiscVAssembler.RiscV;

namespace RiscVAssembler.Decoder;

public class RvaModule : IDisassemblerModule
{
    private readonly Xlen _xlen;

    public RvaModule(Xlen xlen = Xlen.Unknown)
    {
        _xlen = xlen;
    }

    public bool TryDisassemble(uint instruction, out string text)
    {
        if ((instruction & 0x7F) != Opcodes.AMO) { text = string.Empty; return false; }
        var rd = (instruction >> 7) & 0x1F;
        var funct3 = (instruction >> 12) & 0x7;
        var rs1 = (instruction >> 15) & 0x1F;
        var rs2 = (instruction >> 20) & 0x1F;
    var funct7 = (instruction >> 25) & 0x7F; // [31:25] = funct5[4:0] | aq | rl
    var funct5 = (funct7 >> 2) & 0x1F;      // extract upper 5 bits as funct5
        // Validate width by funct3
        if (funct3 != 0b010 && funct3 != 0b011)
        {
            text = "unknown"; return true;
        }
        // RV32A does not have .d encodings
        if (_xlen == Xlen.X32 && funct3 == 0b011)
        {
            text = "unknown"; return true;
        }
        string width = funct3 == 0b010 ? ".w" : ".d";
        string baseName = funct5 switch
        {
            0b00001 => "amoswap", 0b00000 => "amoadd", 0b00100 => "amoxor",
            0b01100 => "amoand", 0b01000 => "amoor",  0b10000 => "amomin",
            0b10100 => "amomax", 0b11000 => "amominu", 0b11100 => "amomaxu",
            0b00010 => (rs2==0?"lr":"unknown"), 0b00011 => "sc",
            _ => "unknown"
        };
    if (baseName == "lr") { text = $"lr{width} {RegisterUtils.RegName((int)rd)}, ({RegisterUtils.RegName((int)rs1)})"; return true; }
    if (baseName == "sc") { text = $"sc{width} {RegisterUtils.RegName((int)rd)}, {RegisterUtils.RegName((int)rs2)}, ({RegisterUtils.RegName((int)rs1)})"; return true; }
    text = $"{baseName}{width} {RegisterUtils.RegName((int)rd)}, {RegisterUtils.RegName((int)rs1)}, {RegisterUtils.RegName((int)rs2)}"; return true;
    }
}
