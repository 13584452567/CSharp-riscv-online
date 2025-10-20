// RiscVAssembler/Assembler/Rv32iAssembler.cs
using RiscVAssembler.RiscV;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace RiscVAssembler.Assembler;

/// <summary>
/// Implements the assembler logic for the RV32I instruction set.
/// </summary>
public class Rv32iAssembler : IRiscVAssemblerModule
{
    private readonly Dictionary<string, Func<Instruction, IEnumerable<uint>>> _instructionHandlers;

    public Rv32iAssembler()
    {
    _instructionHandlers = new Dictionary<string, Func<Instruction, IEnumerable<uint>>>(StringComparer.OrdinalIgnoreCase)
        {
            // R-Type
            { "add", i => new[] { AssembleRType(i, 0b000, 0b0000000) } },
            { "sub", i => new[] { AssembleRType(i, 0b000, 0b0100000) } },
            { "sll", i => new[] { AssembleRType(i, 0b001, 0b0000000) } },
            { "slt", i => new[] { AssembleRType(i, 0b010, 0b0000000) } },
            { "sltu", i => new[] { AssembleRType(i, 0b011, 0b0000000) } },
            { "xor", i => new[] { AssembleRType(i, 0b100, 0b0000000) } },
            { "srl", i => new[] { AssembleRType(i, 0b101, 0b0000000) } },
            { "sra", i => new[] { AssembleRType(i, 0b101, 0b0100000) } },
            { "or", i => new[] { AssembleRType(i, 0b110, 0b0000000) } },
            { "and", i => new[] { AssembleRType(i, 0b111, 0b0000000) } },

            // I-Type (OP_IMM)
            { "addi", i => new[] { AssembleIType(i, Opcodes.OP_IMM, 0b000) } },
            { "slti", i => new[] { AssembleIType(i, Opcodes.OP_IMM, 0b010) } },
            { "sltiu", i => new[] { AssembleIType(i, Opcodes.OP_IMM, 0b011) } },
            { "xori", i => new[] { AssembleIType(i, Opcodes.OP_IMM, 0b100) } },
            { "ori", i => new[] { AssembleIType(i, Opcodes.OP_IMM, 0b110) } },
            { "andi", i => new[] { AssembleIType(i, Opcodes.OP_IMM, 0b111) } },
            
            // I-Type (Shifts)
            { "slli", i => new[] { AssembleShiftIType(i, 0b001, 0b0000000) } },
            { "srli", i => new[] { AssembleShiftIType(i, 0b101, 0b0000000) } },
            { "srai", i => new[] { AssembleShiftIType(i, 0b101, 0b0100000) } },

            // I-Type (Load)
            { "lb", i => new[] { AssembleLoadType(i, 0b000) } },
            { "lh", i => new[] { AssembleLoadType(i, 0b001) } },
            { "lw", i => new[] { AssembleLoadType(i, 0b010) } },
            { "lbu", i => new[] { AssembleLoadType(i, 0b100) } },
            { "lhu", i => new[] { AssembleLoadType(i, 0b101) } },

            // I-Type (JALR)
            { "jalr", i => new[] { AssembleIType(i, Opcodes.JALR, 0b000) } },

            // S-Type
            { "sb", i => new[] { AssembleSType(i, 0b000) } },
            { "sh", i => new[] { AssembleSType(i, 0b001) } },
            { "sw", i => new[] { AssembleSType(i, 0b010) } },

            // B-Type
            { "beq", i => new[] { AssembleBType(i, 0b000) } },
            { "bne", i => new[] { AssembleBType(i, 0b001) } },
            { "blt", i => new[] { AssembleBType(i, 0b100) } },
            { "bge", i => new[] { AssembleBType(i, 0b101) } },
            { "bltu", i => new[] { AssembleBType(i, 0b110) } },
            { "bgeu", i => new[] { AssembleBType(i, 0b111) } },

            // U-Type
            { "lui", i => new[] { AssembleUType(i, Opcodes.LUI) } },
            { "auipc", i => new[] { AssembleUType(i, Opcodes.AUIPC) } },

            // J-Type
            { "jal", i => new[] { AssembleJType(i) } },
            // Pseudo-instruction: j imm -> jal x0, imm
            { "j", i => {
                    if (i.Operands.Length != 1) throw new ArgumentException("j requires imm");
                    int imm = ParseImmediate(i.Operands[0]);
                    return new[] { InstructionBuilder.BuildJType(Opcodes.JAL, 0, imm) };
                }
            },
            // Common pseudo-instructions
            { "li", i => {
                    if (i.Operands.Length != 2) throw new ArgumentException("li requires rd, imm");
                    uint rd = ParseRegister(i.Operands[0]);
                    int imm = ParseImmediate(i.Operands[1]);
                    if (imm >= -2048 && imm <= 2047)
                    {
                        // single addi
                        return new[] { InstructionBuilder.BuildIType(Opcodes.OP_IMM, 0b000, rd, 0, imm) };
                    }
                    // For larger immediates, emit LUI rd, imm_hi and ADDI rd, rd, imm_lo
                    // imm_hi is the 20-bit upper part after rounding the low 12 bits
                    int imm_lo = imm & 0xFFF;
                    // sign-extend imm_lo from 12 bits to determine carry into high part
                    if (imm_lo >= 0x800) imm_lo -= 0x1000;
                    int imm_hi = imm - imm_lo; // multiple of 4096
                    uint lui = InstructionBuilder.BuildUType(Opcodes.LUI, rd, imm_hi);
                    uint addi = InstructionBuilder.BuildIType(Opcodes.OP_IMM, 0b000, rd, rd, imm_lo);
                    return new[] { lui, addi };
                }
            },
            { "mv", i => {
                    if (i.Operands.Length != 2) throw new ArgumentException("mv requires rd, rs");
                    uint rd = ParseRegister(i.Operands[0]);
                    uint rs = ParseRegister(i.Operands[1]);
                    return new[] { InstructionBuilder.BuildIType(Opcodes.OP_IMM, 0b000, rd, rs, 0) }; // addi rd, rs, 0
                }
            },
            // call imm -> jal ra, imm
            { "call", i => {
                    if (i.Operands.Length != 1) throw new ArgumentException("call requires imm");
                    int imm = ParseImmediate(i.Operands[0]);
                    return new[] { InstructionBuilder.BuildJType(Opcodes.JAL, 1, imm) };
                }
            },
            // la rd, imm -> like li but use LUI+ADDI for large immediates
            { "la", i => {
                    if (i.Operands.Length != 2) throw new ArgumentException("la requires rd, imm");
                    uint rd = ParseRegister(i.Operands[0]);
                    int imm = ParseImmediate(i.Operands[1]);
                    if (imm >= -2048 && imm <= 2047)
                        return new[] { InstructionBuilder.BuildIType(Opcodes.OP_IMM, 0b000, rd, 0, imm) };
                    int imm_lo = imm & 0xFFF;
                    if (imm_lo >= 0x800) imm_lo -= 0x1000;
                    int imm_hi = imm - imm_lo;
                    uint lui = InstructionBuilder.BuildUType(Opcodes.LUI, rd, imm_hi);
                    uint addi = InstructionBuilder.BuildIType(Opcodes.OP_IMM, 0b000, rd, rd, imm_lo);
                    return new[] { lui, addi };
                }
            },
            // not rd, rs -> xori rd, rs, -1
            { "not", i => {
                    if (i.Operands.Length != 2) throw new ArgumentException("not requires rd, rs");
                    uint rd = ParseRegister(i.Operands[0]);
                    uint rs = ParseRegister(i.Operands[1]);
                    return new[] { InstructionBuilder.BuildIType(Opcodes.OP_IMM, 0b100, rd, rs, -1) };
                }
            },
            // neg rd, rs -> sub rd, x0, rs
            { "neg", i => {
                    if (i.Operands.Length != 2) throw new ArgumentException("neg requires rd, rs");
                    uint rd = ParseRegister(i.Operands[0]);
                    uint rs = ParseRegister(i.Operands[1]);
                    return new[] { InstructionBuilder.BuildRType(Opcodes.OP, 0b000, 0b0100000, rd, 0, rs) };
                }
            },
            // seqz rd, rs -> sltiu rd, rs, 1
            { "seqz", i => {
                    if (i.Operands.Length != 2) throw new ArgumentException("seqz requires rd, rs");
                    uint rd = ParseRegister(i.Operands[0]);
                    uint rs = ParseRegister(i.Operands[1]);
                    return new[] { InstructionBuilder.BuildIType(Opcodes.OP_IMM, 0b011, rd, rs, 1) };
                }
            },
            // push rs -> addi sp, sp, -4 ; sw rs, 0(sp)
            { "push", i => {
                    if (i.Operands.Length != 1) throw new ArgumentException("push requires rs");
                    uint rs = ParseRegister(i.Operands[0]);
                    // addi sp, sp, -4
                    uint addi = InstructionBuilder.BuildIType(Opcodes.OP_IMM, 0b000, 2, 2, -4);
                    // sw rs, 0(sp)
                    uint sw = InstructionBuilder.BuildSType(Opcodes.STORE, 0b010, 2, rs, 0);
                    return new[] { addi, sw };
                }
            },
            // pop rd -> lw rd, 0(sp) ; addi sp, sp, 4
            { "pop", i => {
                    if (i.Operands.Length != 1) throw new ArgumentException("pop requires rd");
                    uint rd = ParseRegister(i.Operands[0]);
                    uint lw = InstructionBuilder.BuildIType(Opcodes.LOAD, 0b010, rd, 2, 0);
                    uint addi = InstructionBuilder.BuildIType(Opcodes.OP_IMM, 0b000, 2, 2, 4);
                    return new[] { lw, addi };
                }
            },
            // Branch aliases (signed): bgt rs1, rs2, imm -> blt rs2, rs1, imm
            { "bgt", i => {
                    if (i.Operands.Length != 3) throw new ArgumentException("bgt requires rs1, rs2, imm");
                    uint rs1 = ParseRegister(i.Operands[0]);
                    uint rs2 = ParseRegister(i.Operands[1]);
                    int imm = ParseImmediate(i.Operands[2]);
                    // blt rs2, rs1, imm (funct3=100)
                    return new[] { InstructionBuilder.BuildBType(Opcodes.BRANCH, 0b100, rs2, rs1, imm) };
                }
            },
            { "ble", i => {
                    if (i.Operands.Length != 3) throw new ArgumentException("ble requires rs1, rs2, imm");
                    uint rs1 = ParseRegister(i.Operands[0]);
                    uint rs2 = ParseRegister(i.Operands[1]);
                    int imm = ParseImmediate(i.Operands[2]);
                    // bge rs2, rs1, imm (funct3=101)
                    return new[] { InstructionBuilder.BuildBType(Opcodes.BRANCH, 0b101, rs2, rs1, imm) };
                }
            },
            // Unsigned branch aliases
            { "bgtu", i => {
                    if (i.Operands.Length != 3) throw new ArgumentException("bgtu requires rs1, rs2, imm");
                    uint rs1 = ParseRegister(i.Operands[0]);
                    uint rs2 = ParseRegister(i.Operands[1]);
                    int imm = ParseImmediate(i.Operands[2]);
                    // bltu rs2, rs1, imm (funct3=110)
                    return new[] { InstructionBuilder.BuildBType(Opcodes.BRANCH, 0b110, rs2, rs1, imm) };
                }
            },
            { "bleu", i => {
                    if (i.Operands.Length != 3) throw new ArgumentException("bleu requires rs1, rs2, imm");
                    uint rs1 = ParseRegister(i.Operands[0]);
                    uint rs2 = ParseRegister(i.Operands[1]);
                    int imm = ParseImmediate(i.Operands[2]);
                    // bgeu rs2, rs1, imm (funct3=111)
                    return new[] { InstructionBuilder.BuildBType(Opcodes.BRANCH, 0b111, rs2, rs1, imm) };
                }
            },
            // pushm r1,r2,... -> addi sp, sp, -4*n ; sw r1, 0(sp) ; sw r2, 4(sp) ; ...
            { "pushm", i => {
                    if (i.Operands.Length < 1) throw new ArgumentException("pushm requires at least one register");
                    var regs = i.Operands.Select(op => ParseRegister(op)).ToArray();
                    int n = regs.Length;
                    int total = -4 * n;
                    var words = new List<uint>();
                    // adjust sp
                    words.Add(InstructionBuilder.BuildIType(Opcodes.OP_IMM, 0b000, 2, 2, total));
                    // store each reg at offset
                    for (int idx = 0; idx < n; idx++)
                    {
                        int offset = idx * 4;
                        words.Add(InstructionBuilder.BuildSType(Opcodes.STORE, 0b010, 2, regs[idx], offset));
                    }
                    return words;
                }
            },
            // popm r1,r2,... -> lw r1, 0(sp) ; lw r2, 4(sp) ; ... ; addi sp, sp, 4*n
            { "popm", i => {
                    if (i.Operands.Length < 1) throw new ArgumentException("popm requires at least one register");
                    var regs = i.Operands.Select(op => ParseRegister(op)).ToArray();
                    int n = regs.Length;
                    var words = new List<uint>();
                    for (int idx = 0; idx < n; idx++)
                    {
                        int offset = idx * 4;
                        words.Add(InstructionBuilder.BuildIType(Opcodes.LOAD, 0b010, regs[idx], 2, offset));
                    }
                    words.Add(InstructionBuilder.BuildIType(Opcodes.OP_IMM, 0b000, 2, 2, 4 * n));
                    return words;
                }
            },
            { "ret", i => {
                    if (i.Operands.Length != 0) throw new ArgumentException("ret takes no operands");
                    // ret -> jalr x0, ra, 0
                    return new[] { InstructionBuilder.BuildIType(Opcodes.JALR, 0b000, 0, 1, 0) };
                }
            },
            { "nop", i => {
                    // nop -> addi x0, x0, 0 (encoded as addi x0, x0, 0 where rd=0, result discarded)
                    return new[] { InstructionBuilder.BuildIType(Opcodes.OP_IMM, 0b000, 0, 0, 0) };
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
                foreach (var w in handler(instruction))
                    yield return w;
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

    public IReadOnlyDictionary<string, Func<Instruction, IEnumerable<uint>>> GetHandlers() => _instructionHandlers;
}
