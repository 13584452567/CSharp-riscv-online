// RiscVAssembler/Assembler/Rv64iAssembler.cs
using System.Text.RegularExpressions;
using RiscVAssembler.RiscV;

namespace RiscVAssembler.Assembler;

public class Rv64iAssembler : IRiscVAssemblerModule
{
    private readonly Dictionary<string, Func<Instruction, IEnumerable<uint>>> _handlers;

    public Rv64iAssembler()
    {
        _handlers = new(StringComparer.OrdinalIgnoreCase)
        {
            // 64-bit immediate ops
            { "addiw", i => new[] { AssembleITypeW(i, 0b000) } },
            { "slliw", i => new[] { AssembleShiftIW(i, 0b001, 0b0000000) } },
            { "srliw", i => new[] { AssembleShiftIW(i, 0b101, 0b0000000) } },
            { "sraiw", i => new[] { AssembleShiftIW(i, 0b101, 0b0100000) } },

            // Override base shifts with 6-bit shamt support
            { "slli", i => new[] { AssembleShiftI64(i, 0b001, 0b0000000) } },
            { "srli", i => new[] { AssembleShiftI64(i, 0b101, 0b0000000) } },
            { "srai", i => new[] { AssembleShiftI64(i, 0b101, 0b0100000) } },

            // 64-bit register ops
            { "addw",  i => new[] { AssembleRTypeW(i, 0b000, 0b0000000) } },
            { "subw",  i => new[] { AssembleRTypeW(i, 0b000, 0b0100000) } },
            { "sllw",  i => new[] { AssembleRTypeW(i, 0b001, 0b0000000) } },
            { "srlw",  i => new[] { AssembleRTypeW(i, 0b101, 0b0000000) } },
            { "sraw",  i => new[] { AssembleRTypeW(i, 0b101, 0b0100000) } },

            // Pseudo instructions specific to RV64I word operations
            { "negw", i => {
                    if (i.Operands.Length != 2) throw new ArgumentException("negw requires rd, rs");
                    uint rd = ParseRegister(i.Operands[0]);
                    uint rs = ParseRegister(i.Operands[1]);
                    return new[] { InstructionBuilder.BuildRType(Opcodes.OP_32, 0b000, 0b0100000, rd, 0, rs) };
                }
            },
            { "sext.w", i => {
                    if (i.Operands.Length != 2) throw new ArgumentException("sext.w requires rd, rs");
                    uint rd = ParseRegister(i.Operands[0]);
                    uint rs = ParseRegister(i.Operands[1]);
                    return new[] { InstructionBuilder.BuildIType(Opcodes.OP_IMM_32, 0b000, rd, rs, 0) };
                }
            },

            // 64-bit loads/stores
            { "ld", i => new[] { AssembleLoadStore(i, isLoad:true,  funct3:0b011) } },
            { "sd", i => new[] { AssembleLoadStore(i, isLoad:false, funct3:0b011) } },
            { "lwu", i => new[] { AssembleLwu(i) } },
            // M-extension handled by RvmAssembler (centralized implementation)
        };
    }

    public IReadOnlyDictionary<string, Func<Instruction, IEnumerable<uint>>> GetHandlers() => _handlers;

    private uint AssembleITypeW(Instruction instruction, uint funct3)
    {
        if (instruction.Operands.Length != 3)
            throw new ArgumentException("*_IW requires 3 operands");
        uint rd = ParseRegister(instruction.Operands[0]);
        uint rs1 = ParseRegister(instruction.Operands[1]);
        int imm = ParseImmediate(instruction.Operands[2]);
        return InstructionBuilder.BuildIType(Opcodes.OP_IMM_32, funct3, rd, rs1, imm);
    }

    private uint AssembleShiftIW(Instruction instruction, uint funct3, uint funct7)
    {
        if (instruction.Operands.Length != 3)
            throw new ArgumentException("shiftIW requires 3 operands");
        uint rd = ParseRegister(instruction.Operands[0]);
        uint rs1 = ParseRegister(instruction.Operands[1]);
        int shamt = ParseImmediate(instruction.Operands[2]);
        if (shamt < 0 || shamt > 31) throw new ArgumentOutOfRangeException("shamt 0..31");
        uint imm = (uint)shamt | (funct7 << 5);
        return InstructionBuilder.BuildIType(Opcodes.OP_IMM_32, funct3, rd, rs1, (int)imm);
    }

    private uint AssembleShiftI64(Instruction instruction, uint funct3, uint funct7)
    {
        if (instruction.Operands.Length != 3)
            throw new ArgumentException("shiftI requires rd, rs1, shamt");
        uint rd = ParseRegister(instruction.Operands[0]);
        uint rs1 = ParseRegister(instruction.Operands[1]);
        int shamt = ParseImmediate(instruction.Operands[2]);
        if (shamt < 0 || shamt > 63)
            throw new ArgumentOutOfRangeException(nameof(shamt), "Shift amount must be between 0 and 63 for RV64I");
        uint imm = ((uint)shamt & 0x3Fu) | (funct7 << 5);
        return InstructionBuilder.BuildIType(Opcodes.OP_IMM, funct3, rd, rs1, (int)imm);
    }

    private uint AssembleRTypeW(Instruction instruction, uint funct3, uint funct7)
    {
        if (instruction.Operands.Length != 3)
            throw new ArgumentException("RTypeW requires 3 operands");
        uint rd = ParseRegister(instruction.Operands[0]);
        uint rs1 = ParseRegister(instruction.Operands[1]);
        uint rs2 = ParseRegister(instruction.Operands[2]);
        return InstructionBuilder.BuildRType(Opcodes.OP_32, funct3, funct7, rd, rs1, rs2);
    }

    private uint AssembleLoadStore(Instruction instruction, bool isLoad, uint funct3)
    {
        if (instruction.Operands.Length != 2)
            throw new ArgumentException("LD/SD requires rd/rs2, offset(base)");
        if (isLoad)
        {
            uint rd = ParseRegister(instruction.Operands[0]);
            var (rs1, imm) = ParseMemoryOperand(instruction.Operands[1]);
            return InstructionBuilder.BuildIType(Opcodes.LOAD, funct3, rd, rs1, imm);
        }
        else
        {
            uint rs2 = ParseRegister(instruction.Operands[0]);
            var (rs1, imm) = ParseMemoryOperand(instruction.Operands[1]);
            return InstructionBuilder.BuildSType(Opcodes.STORE, funct3, rs1, rs2, imm);
        }
    }

    private uint AssembleLwu(Instruction instruction)
    {
        if (instruction.Operands.Length != 2)
            throw new ArgumentException("lwu requires rd, offset(base)");
        uint rd = ParseRegister(instruction.Operands[0]);
        var (rs1, imm) = ParseMemoryOperand(instruction.Operands[1]);
        return InstructionBuilder.BuildIType(Opcodes.LOAD, 0b110, rd, rs1, imm);
    }

    private (uint register, int offset) ParseMemoryOperand(string operand)
    {
        int open = operand.IndexOf('(');
        int close = operand.IndexOf(')', Math.Max(open + 1, 0));
        if (open < 0 || close < 0 || close <= open + 1)
            throw new ArgumentException($"Invalid memory operand format: {operand}.");

        string offsetToken = operand[..open].Trim();
        string regToken = operand.Substring(open + 1, close - open - 1).Trim();
        if (regToken.Length == 0)
            throw new ArgumentException($"Missing base register in operand {operand}");

        int offset = string.IsNullOrWhiteSpace(offsetToken) ? 0 : ParseImmediate(offsetToken);
        uint register = ParseRegister(regToken);
        return (register, offset);
    }

    private uint ParseRegister(string reg)
    {
        reg = reg.ToLower();
        if (reg.StartsWith("x") && uint.TryParse(reg[1..], out var n))
        {
            if (n > 31) throw new ArgumentException($"Invalid reg x{n}");
            return n;
        }
        return reg switch
        {
            "zero" => 0, "ra" => 1, "sp" => 2, "gp" => 3, "tp" => 4,
            "t0" => 5, "t1" => 6, "t2" => 7,
            "s0" or "fp" => 8, "s1" => 9,
            "a0" => 10, "a1" => 11, "a2" => 12, "a3" => 13, "a4" => 14, "a5" => 15,
            "a6" => 16, "a7" => 17,
            "s2" => 18, "s3" => 19, "s4" => 20, "s5" => 21, "s6" => 22, "s7" => 23,
            "s8" => 24, "s9" => 25, "s10" => 26, "s11" => 27,
            "t3" => 28, "t4" => 29, "t5" => 30, "t6" => 31,
            _ => throw new ArgumentException($"Unknown reg: {reg}")
        };
    }

    private int ParseImmediate(string imm)
    {
        imm = imm.Trim();
        if (AssemblySymbols.Symbols != null && AssemblySymbols.TryResolve(imm, out var symVal))
            return symVal;

        var lower = imm.ToLower();
        if (lower.StartsWith("-0x")) return -Convert.ToInt32(lower[3..], 16);
        if (lower.StartsWith("+0x")) return Convert.ToInt32(lower[3..], 16);
        if (lower.StartsWith("0x")) return Convert.ToInt32(lower, 16);
        if (int.TryParse(lower, out int v)) return v;
        throw new ArgumentException($"Invalid immediate: {imm}");
    }
}
