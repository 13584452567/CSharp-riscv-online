// RiscVAssembler/Assembler/Rv32iAssembler.cs
using RiscVAssembler.RiscV;
using System.Text.RegularExpressions;

namespace RiscVAssembler.Assembler;

/// <summary>
/// Implements the assembler logic for the RV32I instruction set.
/// </summary>
public class Rv32iAssembler : IRiscVAssemblerModule
{
    private readonly Dictionary<string, Func<Instruction, uint>> _instructionHandlers;

    public Rv32iAssembler()
    {
        _instructionHandlers = new Dictionary<string, Func<Instruction, uint>>(StringComparer.OrdinalIgnoreCase)
        {
            // R-Type
            { "add", i => AssembleRType(i, 0b000, 0b0000000) },
            { "sub", i => AssembleRType(i, 0b000, 0b0100000) },
            { "sll", i => AssembleRType(i, 0b001, 0b0000000) },
            { "slt", i => AssembleRType(i, 0b010, 0b0000000) },
            { "sltu", i => AssembleRType(i, 0b011, 0b0000000) },
            { "xor", i => AssembleRType(i, 0b100, 0b0000000) },
            { "srl", i => AssembleRType(i, 0b101, 0b0000000) },
            { "sra", i => AssembleRType(i, 0b101, 0b0100000) },
            { "or", i => AssembleRType(i, 0b110, 0b0000000) },
            { "and", i => AssembleRType(i, 0b111, 0b0000000) },

            // I-Type (OP_IMM)
            { "addi", i => AssembleIType(i, Opcodes.OP_IMM, 0b000) },
            { "slti", i => AssembleIType(i, Opcodes.OP_IMM, 0b010) },
            { "sltiu", i => AssembleIType(i, Opcodes.OP_IMM, 0b011) },
            { "xori", i => AssembleIType(i, Opcodes.OP_IMM, 0b100) },
            { "ori", i => AssembleIType(i, Opcodes.OP_IMM, 0b110) },
            { "andi", i => AssembleIType(i, Opcodes.OP_IMM, 0b111) },
            
            // I-Type (Shifts)
            { "slli", i => AssembleShiftIType(i, 0b001, 0b0000000) },
            { "srli", i => AssembleShiftIType(i, 0b101, 0b0000000) },
            { "srai", i => AssembleShiftIType(i, 0b101, 0b0100000) },

            // I-Type (Load)
            { "lb", i => AssembleLoadType(i, 0b000) },
            { "lh", i => AssembleLoadType(i, 0b001) },
            { "lw", i => AssembleLoadType(i, 0b010) },
            { "lbu", i => AssembleLoadType(i, 0b100) },
            { "lhu", i => AssembleLoadType(i, 0b101) },

            // I-Type (JALR)
            { "jalr", i => AssembleIType(i, Opcodes.JALR, 0b000) },

            // S-Type
            { "sb", i => AssembleSType(i, 0b000) },
            { "sh", i => AssembleSType(i, 0b001) },
            { "sw", i => AssembleSType(i, 0b010) },

            // B-Type
            { "beq", i => AssembleBType(i, 0b000) },
            { "bne", i => AssembleBType(i, 0b001) },
            { "blt", i => AssembleBType(i, 0b100) },
            { "bge", i => AssembleBType(i, 0b101) },
            { "bltu", i => AssembleBType(i, 0b110) },
            { "bgeu", i => AssembleBType(i, 0b111) },

            // U-Type
            { "lui", i => AssembleUType(i, Opcodes.LUI) },
            { "auipc", i => AssembleUType(i, Opcodes.AUIPC) },

            // J-Type
            { "jal", AssembleJType },
            // Pseudo-instruction: j imm -> jal x0, imm
            { "j", i => {
                    if (i.Operands.Length != 1) throw new ArgumentException("j requires imm");
                    int imm = ParseImmediate(i.Operands[0]);
                    return InstructionBuilder.BuildJType(Opcodes.JAL, 0, imm);
                }
            },
            // Common pseudo-instructions
            { "li", i => {
                    // li rd, imm -> if imm fits in 12-bit signed -> addi rd, x0, imm
                    // else -> lui rd, imm_hi << 12 ; addi rd, rd, imm_lo
                    if (i.Operands.Length != 2) throw new ArgumentException("li requires rd, imm");
                    uint rd = ParseRegister(i.Operands[0]);
                    int imm = ParseImmediate(i.Operands[1]);
                    if (imm >= -2048 && imm <= 2047)
                    {
                        return InstructionBuilder.BuildIType(Opcodes.OP_IMM, 0b000, rd, 0, imm); // addi rd, x0, imm
                    }
                    throw new NotSupportedException("li for large immediates (outside -2048..2047) is not supported by this assembler handler.");
                }
            },
            { "mv", i => {
                    if (i.Operands.Length != 2) throw new ArgumentException("mv requires rd, rs");
                    uint rd = ParseRegister(i.Operands[0]);
                    uint rs = ParseRegister(i.Operands[1]);
                    return InstructionBuilder.BuildIType(Opcodes.OP_IMM, 0b000, rd, rs, 0); // addi rd, rs, 0
                }
            },
            { "ret", i => {
                    if (i.Operands.Length != 0) throw new ArgumentException("ret takes no operands");
                    // ret -> jalr x0, ra, 0
                    return InstructionBuilder.BuildIType(Opcodes.JALR, 0b000, 0, 1, 0);
                }
            },
            { "nop", i => {
                    // nop -> addi x0, x0, 0 (encoded as addi x0, x0, 0 where rd=0, result discarded)
                    return InstructionBuilder.BuildIType(Opcodes.OP_IMM, 0b000, 0, 0, 0);
                }
            },
        };
    }

    /// <summary>
    /// Assembles a block of RISC-V assembly code into machine code.
    /// </summary>
    public IEnumerable<uint> Assemble(string code)
    {
        var lines = code.Split(new[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(line => line.Trim())
                        .Where(line => !string.IsNullOrWhiteSpace(line));

        foreach (var line in lines)
        {
            var instruction = Instruction.Parse(line);
            if (_instructionHandlers.TryGetValue(instruction.Mnemonic, out var handler))
            {
                yield return handler(instruction);
            }
            else
            {
                throw new NotSupportedException($"Instruction '{instruction.Mnemonic}' is not supported.");
            }
        }
    }

    #region Instruction Type Assemblers

    private uint AssembleRType(Instruction instruction, uint funct3, uint funct7)
    {
        if (instruction.Operands.Length != 3)
            throw new ArgumentException($"{instruction.Mnemonic.ToUpper()} instruction requires 3 operands.");

        uint rd = ParseRegister(instruction.Operands[0]);
        uint rs1 = ParseRegister(instruction.Operands[1]);
        uint rs2 = ParseRegister(instruction.Operands[2]);

        return InstructionBuilder.BuildRType(Opcodes.OP, funct3, funct7, rd, rs1, rs2);
    }

    private uint AssembleIType(Instruction instruction, uint opcode, uint funct3)
    {
        if (instruction.Operands.Length != 3)
            throw new ArgumentException($"{instruction.Mnemonic.ToUpper()} instruction requires 3 operands.");

        uint rd = ParseRegister(instruction.Operands[0]);
        uint rs1 = ParseRegister(instruction.Operands[1]);
        int imm = ParseImmediate(instruction.Operands[2]);

        return InstructionBuilder.BuildIType(opcode, funct3, rd, rs1, imm);
    }
    
    private uint AssembleShiftIType(Instruction instruction, uint funct3, uint funct7)
    {
        if (instruction.Operands.Length != 3)
            throw new ArgumentException($"{instruction.Mnemonic.ToUpper()} instruction requires 3 operands.");

        uint rd = ParseRegister(instruction.Operands[0]);
        uint rs1 = ParseRegister(instruction.Operands[1]);
        int shamt = ParseImmediate(instruction.Operands[2]);

        if (shamt < 0 || shamt > 31)
            throw new ArgumentOutOfRangeException("Shift amount (shamt) must be between 0 and 31.");

        // SLLI, SRLI, SRAI are I-type but encode shamt in the immediate field, and SRAI has a specific funct7.
        uint imm = (uint)shamt | (funct7 << 5);
        return InstructionBuilder.BuildIType(Opcodes.OP_IMM, funct3, rd, rs1, (int)imm);
    }

    private uint AssembleLoadType(Instruction instruction, uint funct3)
    {
        if (instruction.Operands.Length != 2)
            throw new ArgumentException($"{instruction.Mnemonic.ToUpper()} instruction requires 2 operands.");
        
        uint rd = ParseRegister(instruction.Operands[0]);
        var (rs1, imm) = ParseMemoryOperand(instruction.Operands[1]);

        return InstructionBuilder.BuildIType(Opcodes.LOAD, funct3, rd, rs1, imm);
    }

    private uint AssembleSType(Instruction instruction, uint funct3)
    {
        if (instruction.Operands.Length != 2)
            throw new ArgumentException($"{instruction.Mnemonic.ToUpper()} instruction requires 2 operands.");

        uint rs2 = ParseRegister(instruction.Operands[0]);
        var (rs1, imm) = ParseMemoryOperand(instruction.Operands[1]);

        return InstructionBuilder.BuildSType(Opcodes.STORE, funct3, rs1, rs2, imm);
    }

    private uint AssembleBType(Instruction instruction, uint funct3)
    {
        if (instruction.Operands.Length != 3)
            throw new ArgumentException($"{instruction.Mnemonic.ToUpper()} instruction requires 3 operands.");

        uint rs1 = ParseRegister(instruction.Operands[0]);
        uint rs2 = ParseRegister(instruction.Operands[1]);
        int imm = ParseImmediate(instruction.Operands[2]); // Typically a label, but we'll use immediate for now

        return InstructionBuilder.BuildBType(Opcodes.BRANCH, funct3, rs1, rs2, imm);
    }

    private uint AssembleUType(Instruction instruction, uint opcode)
    {
        if (instruction.Operands.Length != 2)
            throw new ArgumentException($"{instruction.Mnemonic.ToUpper()} instruction requires 2 operands.");

        uint rd = ParseRegister(instruction.Operands[0]);
        int imm = ParseImmediate(instruction.Operands[1]);

        return InstructionBuilder.BuildUType(opcode, rd, imm);
    }

    private uint AssembleJType(Instruction instruction)
    {
        if (instruction.Operands.Length != 2)
            throw new ArgumentException("JAL instruction requires 2 operands.");

        uint rd = ParseRegister(instruction.Operands[0]);
        int imm = ParseImmediate(instruction.Operands[1]); // Typically a label

        return InstructionBuilder.BuildJType(Opcodes.JAL, rd, imm);
    }

    #endregion

    #region Parsers

    private (uint register, int offset) ParseMemoryOperand(string operand)
    {
        // Support both xN and ABI names inside parentheses, e.g., 4(x2) or 4(sp)
        var match = Regex.Match(operand, @"(-?\d+)\(([^)]+)\)");
        if (!match.Success)
            throw new ArgumentException($"Invalid memory operand format: '{operand}'. Expected 'offset(reg)'. Examples: 0(x0), 4(sp), 8(s0).");

        var offsetStr = match.Groups[1].Value;
        if (!int.TryParse(offsetStr, out int offset))
            throw new ArgumentException($"Invalid offset value '{offsetStr}' in memory operand '{operand}'. Expected a signed integer, e.g., -4, 0, 16.");

        string regToken = match.Groups[2].Value.Trim();
        if (string.IsNullOrWhiteSpace(regToken))
            throw new ArgumentException($"Missing register in memory operand '{operand}'. Use x0..x31 or ABI names (zero, ra, sp, gp, tp, t0..t6, s0/fp..s11, a0..a7).");

        try
        {
            uint register = ParseRegister(regToken);
            return (register, offset);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"Unknown or invalid register '{regToken}' in memory operand '{operand}'. Use x0..x31 or ABI names (zero, ra, sp, gp, tp, t0..t6, s0/fp..s11, a0..a7).", ex);
        }
    }

    private uint ParseRegister(string reg)
    {
        var match = Regex.Match(reg.ToLower(), @"x(\d+)|([a-z0-9]+)");
        if (!match.Success)
            throw new ArgumentException($"Invalid register format: {reg}");

        if (uint.TryParse(match.Groups[1].Value, out uint regNum))
        {
            if (regNum > 31) throw new ArgumentException($"Invalid register number: x{regNum}");
            return regNum;
        }
        
        string abiName = match.Groups[2].Value;
        return abiName switch
        {
            "zero" => 0, "ra" => 1, "sp" => 2, "gp" => 3,
            "tp" => 4, "t0" => 5, "t1" => 6, "t2" => 7,
            "s0" or "fp" => 8, "s1" => 9,
            "a0" => 10, "a1" => 11, "a2" => 12, "a3" => 13, "a4" => 14, "a5" => 15,
            "a6" => 16, "a7" => 17,
            "s2" => 18, "s3" => 19, "s4" => 20, "s5" => 21, "s6" => 22, "s7" => 23,
            "s8" => 24, "s9" => 25, "s10" => 26, "s11" => 27,
            "t3" => 28, "t4" => 29, "t5" => 30, "t6" => 31,
            _ => throw new ArgumentException($"Unknown register ABI name: {abiName}")
        };
    }

    private int ParseImmediate(string imm)
    {
        imm = imm.ToLower();
        if (imm.StartsWith("0x"))
        {
            return Convert.ToInt32(imm, 16);
        }
        if (int.TryParse(imm, out int value))
        {
            return value;
        }
        throw new ArgumentException($"Invalid immediate value: {imm}");
    }

    #endregion

    public IReadOnlyDictionary<string, Func<Instruction, uint>> GetHandlers() => _instructionHandlers;
}
