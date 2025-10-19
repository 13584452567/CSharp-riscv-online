// RiscVAssembler/Assembler/Rv64iAssembler.cs
using System.Text.RegularExpressions;
using RiscVAssembler.RiscV;

namespace RiscVAssembler.Assembler;

public class Rv64iAssembler : IRiscVAssemblerModule
{
    private readonly Dictionary<string, Func<Instruction, uint>> _handlers;

    public Rv64iAssembler()
    {
        _handlers = new(StringComparer.OrdinalIgnoreCase)
        {
            // 64-bit immediate ops
            { "addiw", i => AssembleITypeW(i, 0b000) },
            { "slliw", i => AssembleShiftIW(i, 0b001, 0b0000000) },
            { "srliw", i => AssembleShiftIW(i, 0b101, 0b0000000) },
            { "sraiw", i => AssembleShiftIW(i, 0b101, 0b0100000) },

            // 64-bit register ops
            { "addw",  i => AssembleRTypeW(i, 0b000, 0b0000000) },
            { "subw",  i => AssembleRTypeW(i, 0b000, 0b0100000) },
            { "sllw",  i => AssembleRTypeW(i, 0b001, 0b0000000) },
            { "srlw",  i => AssembleRTypeW(i, 0b101, 0b0000000) },
            { "sraw",  i => AssembleRTypeW(i, 0b101, 0b0100000) },

            // 64-bit loads/stores
            { "ld", i => AssembleLoadStore(i, isLoad:true,  funct3:0b011) },
            { "sd", i => AssembleLoadStore(i, isLoad:false, funct3:0b011) },
        };
    }

    public IReadOnlyDictionary<string, Func<Instruction, uint>> GetHandlers() => _handlers;

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

    private (uint register, int offset) ParseMemoryOperand(string operand)
    {
        var match = Regex.Match(operand, @"(-?\d+)\(x(\d+)\)");
        if (!match.Success)
            throw new ArgumentException($"Invalid memory operand format: {operand}.");
        int offset = int.Parse(match.Groups[1].Value);
        uint register = uint.Parse(match.Groups[2].Value);
        if (register > 31) throw new ArgumentException($"Invalid register x{register}");
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
        imm = imm.ToLower();
        if (imm.StartsWith("0x")) return Convert.ToInt32(imm, 16);
        if (int.TryParse(imm, out int v)) return v;
        throw new ArgumentException($"Invalid immediate: {imm}");
    }
}
