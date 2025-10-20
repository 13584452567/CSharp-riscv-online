// RiscVAssembler/Assembler/RvcAssembler.cs
using System.Text.RegularExpressions;
using RiscVAssembler.RiscV;

namespace RiscVAssembler.Assembler;

// Minimal RVC assembler: encodes a practical subset matching our RVC disassembler mappings.
public class RvcAssembler : IRiscVAssemblerModule
{
    private readonly Dictionary<string, Func<Instruction, IEnumerable<uint>>> _handlers;

    public RvcAssembler()
    {
    _handlers = new(StringComparer.OrdinalIgnoreCase)
        {
            // Quadrant 0
            { "c.addi4spn", i => new[] { AssembleCAddi4Spn(i) } },
            { "c.lw", i => new[] { AssembleCLw(i) } },
            { "c.sw", i => new[] { AssembleCSw(i) } },
            { "c.ld", i => new[] { AssembleCLd(i) } },
            { "c.sd", i => new[] { AssembleCSd(i) } },
            // compressed floating-point (single/double)
            { "c.flw", i => new[] { AssembleCFlw(i) } },
            { "c.fsw", i => new[] { AssembleCFsw(i) } },
            { "c.fld", i => new[] { AssembleCFld(i) } },
            { "c.fsd", i => new[] { AssembleCFsd(i) } },

            // Quadrant 1
            { "c.nop", i => new[] { AssembleCNoOpOrAddi(i) } },
            { "c.addi", i => new[] { AssembleCNoOpOrAddi(i) } },
            { "c.li", i => new[] { AssembleCLi(i) } },
            { "c.lui", i => new[] { AssembleCLuiOrAddi16Sp(i) } },
            { "c.addi16sp", i => new[] { AssembleCLuiOrAddi16Sp(i) } },
            { "c.srli", i => new[] { AssembleCShiftAndLogic(i) } },
            { "c.srai", i => new[] { AssembleCShiftAndLogic(i) } },
            { "c.andi", i => new[] { AssembleCShiftAndLogic(i) } },
            { "c.sub", i => new[] { AssembleCArith(i) } },
            { "c.xor", i => new[] { AssembleCArith(i) } },
            { "c.or", i => new[] { AssembleCArith(i) } },
            { "c.and", i => new[] { AssembleCArith(i) } },
            { "c.addiw", i => new[] { AssembleCAddiw(i) } },
            { "c.addw", i => new[] { AssembleCArithW(i) } },
            { "c.subw", i => new[] { AssembleCArithW(i) } },
            { "c.j", i => new[] { AssembleCJ(i) } },
            { "c.jal", i => new[] { AssembleCJ(i) } },
            { "c.beqz", i => new[] { AssembleCBeqzBnez(i) } },
            { "c.bnez", i => new[] { AssembleCBeqzBnez(i) } },

            // Quadrant 2
            { "c.slli", i => new[] { AssembleCSlli(i) } },
            { "c.lwsp", i => new[] { AssembleCLwSp(i) } },
            { "c.swsp", i => new[] { AssembleCSwSp(i) } },
            { "c.ldsp", i => new[] { AssembleCLdSp(i) } },
            { "c.sdsp", i => new[] { AssembleCSdSp(i) } },
            { "c.flwsp", i => new[] { AssembleCFlwSp(i) } },
            { "c.fswsp", i => new[] { AssembleCFswSp(i) } },
            { "c.fldsp", i => new[] { AssembleCFldSp(i) } },
            { "c.fsdsp", i => new[] { AssembleCFsdSp(i) } },
            { "c.jr", i => new[] { AssembleCJrJalrMvAdd(i) } },
            { "c.jalr", i => new[] { AssembleCJrJalrMvAdd(i) } },
            { "c.mv", i => new[] { AssembleCJrJalrMvAdd(i) } },
            { "c.add", i => new[] { AssembleCJrJalrMvAdd(i) } },
            { "c.ebreak", i => new[] { AssembleCEbreak(i) } },
        };
    }

    public IReadOnlyDictionary<string, Func<Instruction, IEnumerable<uint>>> GetHandlers() => _handlers;

    // Quadrant 1: c.nop / c.addi rd, imm  (funct3=000)
    private static uint AssembleCNoOpOrAddi(Instruction ins)
    {
        ushort w = 0;
        int rd, imm;
        if (ins.Mnemonic.Equals("c.nop", StringComparison.OrdinalIgnoreCase))
        {
            rd = 0; imm = 0;
        }
        else
        {
            if (ins.Operands.Length != 2) throw new ArgumentException("c.addi requires rd, imm");
            rd = (int)ParseGpr(ins.Operands[0]);
            imm = ParseImm(ins.Operands[1]);
        }
        if (imm < -32 || imm > 31) throw new ArgumentOutOfRangeException("c.addi imm must be 6-bit signed");
        w |= (ushort)(0b000 << 13);
        w |= EncodeImm6(imm);
        w |= (ushort)(rd << 7);
        w |= 0b01; // quadrant 1
        return w;
    }

    // Quadrant 1: c.addiw rd, imm (RV64C) (funct3=001)
    private static uint AssembleCAddiw(Instruction ins)
    {
        if (ins.Operands.Length != 2) throw new ArgumentException("c.addiw requires rd, imm");
        int rd = (int)ParseGpr(ins.Operands[0]);
        int imm = ParseImm(ins.Operands[1]);
        if (imm < -32 || imm > 31) throw new ArgumentOutOfRangeException("c.addiw imm must be 6-bit signed");
        ushort w = 0;
        w |= (ushort)(0b001 << 13);
        w |= EncodeImm6(imm);
        w |= (ushort)(rd << 7);
        w |= 0b01; // quadrant 1
        return w;
    }

    // Quadrant 1: c.srli / c.srai / c.andi (funct3=100, with bits[11:10]=00/01/10)
    private static uint AssembleCShiftAndLogic(Instruction ins)
    {
        if (ins.Mnemonic.Equals("c.andi", StringComparison.OrdinalIgnoreCase))
        {
            if (ins.Operands.Length != 2) throw new ArgumentException("c.andi requires rd', imm");
            int rdPrime = (int)ParseGprPrime(ins.Operands[0]);
            int imm = ParseImm(ins.Operands[1]);
            if (imm < -32 || imm > 31) throw new ArgumentOutOfRangeException("c.andi imm must be 6-bit signed");
            ushort w = 0;
            w |= (ushort)(0b100 << 13);
            w |= (ushort)(rdPrime << 7);
            // selector 0b10 in bits [11:10]
            w |= (ushort)(0b10 << 10);
            w |= EncodeImm6(imm);
            w |= 0b01; // quadrant 1
            return w;
        }
        else
        {
            if (ins.Operands.Length != 2) throw new ArgumentException("c.srli/c.srai requires rd', shamt");
            int rdPrime = (int)ParseGprPrime(ins.Operands[0]);
            int shamt = ParseImm(ins.Operands[1]);
            if (shamt < 0 || shamt > 63) throw new ArgumentOutOfRangeException("shift amount must be 0..63");
            ushort w = 0;
            w |= (ushort)(0b100 << 13);
            w |= (ushort)(rdPrime << 7);
            w |= EncodeImm6(shamt);
            // selector in bits [11:10]
            int sel = ins.Mnemonic.Equals("c.srai", StringComparison.OrdinalIgnoreCase) ? 0b01 : 0b00;
            w |= (ushort)(sel << 10);
            w |= 0b01; // quadrant 1
            return w;
        }
    }

    // Quadrant 1: c.sub / c.xor / c.or / c.and (funct3=100, bits[11:10]=11, bits[6:5] select op)
    private static uint AssembleCArith(Instruction ins)
    {
        if (ins.Operands.Length != 2) throw new ArgumentException("c.sub/xor/or/and requires rd', rs2'");
        int rdPrime = (int)ParseGprPrime(ins.Operands[0]);
        int rs2Prime = (int)ParseGprPrime(ins.Operands[1]);
        ushort w = 0;
        w |= (ushort)(0b100 << 13);
        w |= (ushort)(rdPrime << 7);
        w |= (ushort)(rs2Prime << 2);
        // bits [11:10] = 0b11
        w |= (ushort)(0b11 << 10);
        // op select in bits [6:5]
        int op = ins.Mnemonic.ToLower() switch
        {
            "c.sub" => 0b00,
            "c.xor" => 0b01,
            "c.or"  => 0b10,
            "c.and" => 0b11,
            _ => throw new NotSupportedException()
        };
        w |= (ushort)(op << 5);
        // bit12 must be 0 for these (we leave it as 0)
        w |= 0b01; // quadrant 1
        return w;
    }

    // Quadrant 1: c.li rd, imm  (funct3=010)
    private static uint AssembleCLi(Instruction ins)
    {
        if (ins.Operands.Length != 2) throw new ArgumentException("c.li requires rd, imm");
        int rd = (int)ParseGpr(ins.Operands[0]);
        int imm = ParseImm(ins.Operands[1]);
        if (imm < -32 || imm > 31) throw new ArgumentOutOfRangeException("c.li imm must be 6-bit signed");
        ushort w = 0;
        w |= (ushort)(0b010 << 13);
        w |= EncodeImm6(imm);
        w |= (ushort)(rd << 7);
        w |= 0b01;
        return w;
    }

    // Quadrant 1: c.j / c.jal  (funct3=101/001)
    private static uint AssembleCJ(Instruction ins)
    {
        if (ins.Operands.Length != 1) throw new ArgumentException("c.j/c.jal requires imm");
        int imm = ParseImm(ins.Operands[0]);
        ushort w = 0;
        int funct3 = ins.Mnemonic.Equals("c.jal", StringComparison.OrdinalIgnoreCase) ? 0b001 : 0b101;
        w |= (ushort)(funct3 << 13);
        // Inverse mapping of Decoder.RvcModule.GetJImm11
        w |= (ushort)(((imm >> 11) & 0x1) << 12);
        w |= (ushort)(((imm >> 4) & 0x1) << 11);
        w |= (ushort)(((imm >> 8) & 0x3) << 9);
        w |= (ushort)(((imm >> 10) & 0x1) << 8);
        w |= (ushort)(((imm >> 6) & 0x1) << 6);
        w |= (ushort)(((imm >> 7) & 0x1) << 7);
        w |= (ushort)(((imm >> 3) & 0x7) << 3);
        w |= (ushort)(((imm >> 5) & 0x1) << 2);
        w |= 0b01;
        return w;
    }

    // Quadrant 1: c.lui (rd!=x0/x2) or c.addi16sp (rd==x2), funct3=011
    private static uint AssembleCLuiOrAddi16Sp(Instruction ins)
    {
        ushort w = 0;
        w |= (ushort)(0b011 << 13);
        if (ins.Mnemonic.Equals("c.addi16sp", StringComparison.OrdinalIgnoreCase))
        {
            // c.addi16sp imm (must be non-zero multiple of 16)
            if (ins.Operands.Length != 1) throw new ArgumentException("c.addi16sp requires imm");
            int imm = ParseImm(ins.Operands[0]);
            if (imm == 0 || (imm & 0xF) != 0) throw new ArgumentOutOfRangeException("c.addi16sp imm must be non-zero and 16-byte aligned");
            int u = (imm >> 4);
            int b9 = (u >> 5) & 0x1;
            int b8 = (u >> 4) & 0x1;
            int b7 = (u >> 3) & 0x1;
            int b6 = (u >> 2) & 0x1;
            int b5 = (u >> 1) & 0x1;
            int b4 = (u >> 0) & 0x1;
            w |= (ushort)(2 << 7); // rd = x2
            w |= (ushort)(b9 << 12);
            w |= (ushort)(b8 << 4);
            w |= (ushort)(b7 << 3);
            w |= (ushort)(b6 << 5);
            w |= (ushort)(b5 << 2);
            w |= (ushort)(b4 << 6);
            w |= 0b01;
            return w;
        }
        else // c.lui rd, imm
        {
            if (ins.Operands.Length != 2) throw new ArgumentException("c.lui requires rd, imm");
            int rd = (int)ParseGpr(ins.Operands[0]);
            if (rd == 0 || rd == 2) throw new ArgumentException("c.lui rd cannot be x0 or x2");
            int imm = ParseImm(ins.Operands[1]);
            if (imm == 0) throw new ArgumentOutOfRangeException("c.lui imm must be non-zero");
            // Reuse 6-bit signed immediate encoding like c.li/c.addi for simplicity (matches our disassembler behavior)
            w |= EncodeImm6(imm);
            w |= (ushort)(rd << 7);
            w |= 0b01;
            return w;
        }
    }

    // Quadrant 1: c.beqz / c.bnez  (funct3=110/111)
    private static uint AssembleCBeqzBnez(Instruction ins)
    {
        if (ins.Operands.Length != 2) throw new ArgumentException("c.beqz/c.bnez requires rs1', imm");
        int rs1p = (int)ParseGprPrime(ins.Operands[0]);
        int imm = ParseImm(ins.Operands[1]);
        ushort w = 0;
        w |= (ushort)((ins.Mnemonic.Equals("c.bnez", StringComparison.OrdinalIgnoreCase) ? 0b111 : 0b110) << 13);
        w |= 0b01;
        w |= (ushort)(rs1p << 7);
        // Inverse mapping of Decoder.RvcModule.GetBImm9
        w |= (ushort)(((imm >> 8) & 0x1) << 12);
        w |= (ushort)(((imm >> 6) & 0x3) << 10);
        w |= (ushort)(((imm >> 5) & 0x1) << 5);
        w |= (ushort)(((imm >> 3) & 0x3) << 3);
        w |= (ushort)(((imm >> 1) & 0x1) << 2);
        return w;
    }

    // Quadrant 2: c.slli rd, shamt  (funct3=000)
    private static uint AssembleCSlli(Instruction ins)
    {
        if (ins.Operands.Length != 2) throw new ArgumentException("c.slli requires rd, shamt");
        int rd = (int)ParseGpr(ins.Operands[0]);
        int shamt = ParseImm(ins.Operands[1]);
        if (shamt < 0 || shamt > 31) throw new ArgumentOutOfRangeException("c.slli shamt 0..31");
        ushort w = 0;
        w |= (ushort)(0b000 << 13);
        w |= EncodeImm6(shamt);
        w |= (ushort)(rd << 7);
        w |= 0b10; // quadrant 2
        return w;
    }

    // Quadrant 2: c.jr / c.jalr / c.mv / c.add  (funct3=100)
    private static uint AssembleCJrJalrMvAdd(Instruction ins)
    {
        ushort w = 0;
        w |= (ushort)(0b100 << 13);
        w |= 0b10;
        if (ins.Mnemonic.Equals("c.jr", StringComparison.OrdinalIgnoreCase))
        {
            if (ins.Operands.Length != 1) throw new ArgumentException("c.jr requires rs1");
            int rs1 = (int)ParseGpr(ins.Operands[0]);
            w |= (ushort)(rs1 << 7);
            return w;
        }
        if (ins.Mnemonic.Equals("c.jalr", StringComparison.OrdinalIgnoreCase))
        {
            if (ins.Operands.Length != 1) throw new ArgumentException("c.jalr requires rs1");
            int rs1 = (int)ParseGpr(ins.Operands[0]);
            w |= (ushort)(rs1 << 7);
            w |= (1 << 12);
            return w;
        }
        if (ins.Mnemonic.Equals("c.mv", StringComparison.OrdinalIgnoreCase) || ins.Mnemonic.Equals("c.add", StringComparison.OrdinalIgnoreCase))
        {
            if (ins.Operands.Length != 2) throw new ArgumentException("c.mv/c.add requires rd, rs2");
            int rd = (int)ParseGpr(ins.Operands[0]);
            int rs2 = (int)ParseGpr(ins.Operands[1]);
            if (rd == 0 || rs2 == 0) throw new ArgumentException("rd/rs2 cannot be x0 for c.mv/c.add");
            w |= (ushort)(rd << 7);
            w |= (ushort)(rs2 << 2);
            if (ins.Mnemonic.Equals("c.add", StringComparison.OrdinalIgnoreCase)) w |= (1 << 12);
            return w;
        }
        throw new NotSupportedException($"Unsupported mnemonic {ins.Mnemonic}");
    }

    // Quadrant 2: c.ebreak  (funct3=100, rd=0, rs2=0, bit12=1)
    private static uint AssembleCEbreak(Instruction _)
    {
        ushort w = 0;
        w |= (ushort)(0b100 << 13);
        w |= (1 << 12);
        w |= 0b10;
        return w;
    }

    // Quadrant 0: c.lw rd', uimm(rs1')  (funct3=010)
    private static uint AssembleCLw(Instruction ins)
    {
        if (ins.Operands.Length != 2) throw new ArgumentException("c.lw requires rd', uimm(rs1')");
        int rd = (int)ParseGprPrime(ins.Operands[0]);
        var (rs1p, uimm) = ParseMemPrime(ins.Operands[1]);
        ushort w = 0;
        w |= (ushort)(0b010 << 13);
        w |= (ushort)(rd << 2);      // rd' in 4..2
        w |= (ushort)(rs1p << 7);    // rs1' in 9..7
        // match Decoder mapping: bit6->ins5, bit2->ins6, bits[5:3]->ins[10:8]
        w |= (ushort)(((uimm >> 2) & 0x1) << 6);
        w |= (ushort)(((uimm >> 3) & 0x7) << 10);
        w |= (ushort)(((uimm >> 6) & 0x1) << 5);
        w |= 0b00;
        return w;
    }

    // Quadrant 0: c.flw rd', uimm(rs1')  (funct3=010) - same encoding as c.lw but operands are FPRs
    private static uint AssembleCFlw(Instruction ins)
    {
        if (ins.Operands.Length != 2) throw new ArgumentException("c.flw requires rd', uimm(rs1')");
        // Accept fpr names for destination
        uint fd = ParseFprPrime(ins.Operands[0]);
        var (rs1p, uimm) = ParseMemPrime(ins.Operands[1]);
        // Use same bit placements as CLw but destination is encoded in rd' field
        ushort w = 0;
        w |= (ushort)(0b010 << 13);
        w |= (ushort)((fd & 0x7) << 2);
        w |= (ushort)(rs1p << 7);
        w |= (ushort)(((uimm >> 2) & 0x1) << 6);
        w |= (ushort)(((uimm >> 3) & 0x7) << 10);
        w |= (ushort)(((uimm >> 6) & 0x1) << 5);
        w |= 0b00;
        return w;
    }

    // Quadrant 0: c.fsw rs2', uimm(rs1')  (funct3=110) - same encoding as c.sw but operands are FPRs
    private static uint AssembleCFsw(Instruction ins)
    {
        if (ins.Operands.Length != 2) throw new ArgumentException("c.fsw requires rs2', uimm(rs1')");
        uint fs2 = ParseFprPrime(ins.Operands[0]);
        var (rs1p, uimm) = ParseMemPrime(ins.Operands[1]);
        ushort w = 0;
        w |= (ushort)(0b110 << 13);
        w |= (ushort)((fs2 & 0x7) << 2);
        w |= (ushort)(rs1p << 7);
        w |= (ushort)(((uimm >> 2) & 0x1) << 6);
        w |= (ushort)(((uimm >> 3) & 0x7) << 10);
        w |= (ushort)(((uimm >> 6) & 0x1) << 5);
        w |= 0b00;
        return w;
    }


    // Quadrant 0: c.ld rd', uimm(rs1') (funct3=011)
    private static uint AssembleCLd(Instruction ins)
    {
        if (ins.Operands.Length != 2) throw new ArgumentException("c.ld requires rd', uimm(rs1')");
        int rd = (int)ParseGprPrime(ins.Operands[0]);
        var (rs1p, uimm) = ParseMemPrime(ins.Operands[1]);
        ushort w = 0;
        w |= (ushort)(0b011 << 13);
        w |= (ushort)(rd << 2);
        w |= (ushort)(rs1p << 7);
        // uimm[5:3] -> ins[12:10], uimm[7:6] -> ins[6:5]
        w |= (ushort)(((uimm >> 3) & 0x7) << 10);
        w |= (ushort)(((uimm >> 6) & 0x3) << 5);
        w |= 0b00;
        return w;
    }

    // Quadrant 0: c.fld rd', uimm(rs1')  (funct3=011) - double-precision load into FPR prime
    private static uint AssembleCFld(Instruction ins)
    {
        if (ins.Operands.Length != 2) throw new ArgumentException("c.fld requires rd', uimm(rs1')");
        uint fd = ParseFprPrime(ins.Operands[0]);
        var (rs1p, uimm) = ParseMemPrime(ins.Operands[1]);
        ushort w = 0;
        w |= (ushort)(0b011 << 13);
        w |= (ushort)((fd & 0x7) << 2);
        w |= (ushort)(rs1p << 7);
        w |= (ushort)(((uimm >> 3) & 0x7) << 10);
        w |= (ushort)(((uimm >> 6) & 0x3) << 5);
        w |= 0b00;
        return w;
    }

    // Quadrant 0: c.fsd rs2', uimm(rs1')  (funct3=111) - double-precision store from FPR prime
    private static uint AssembleCFsd(Instruction ins)
    {
        if (ins.Operands.Length != 2) throw new ArgumentException("c.fsd requires rs2', uimm(rs1')");
        uint fs2 = ParseFprPrime(ins.Operands[0]);
        var (rs1p, uimm) = ParseMemPrime(ins.Operands[1]);
        ushort w = 0;
        w |= (ushort)(0b111 << 13);
        w |= (ushort)((fs2 & 0x7) << 2);
        w |= (ushort)(rs1p << 7);
        w |= (ushort)(((uimm >> 3) & 0x7) << 10);
        w |= (ushort)(((uimm >> 6) & 0x3) << 5);
        w |= 0b00;
        return w;
    }

    // Quadrant 0: c.sd rs2', uimm(rs1') (funct3=111)
    private static uint AssembleCSd(Instruction ins)
    {
        if (ins.Operands.Length != 2) throw new ArgumentException("c.sd requires rs2', uimm(rs1')");
        int rs2 = (int)ParseGprPrime(ins.Operands[0]);
        var (rs1p, uimm) = ParseMemPrime(ins.Operands[1]);
        ushort w = 0;
        w |= (ushort)(0b111 << 13);
        w |= (ushort)(rs2 << 2);
        w |= (ushort)(rs1p << 7);
        // uimm[5:3] -> ins[12:10], uimm[7:6] -> ins[6:5]
        w |= (ushort)(((uimm >> 3) & 0x7) << 10);
        w |= (ushort)(((uimm >> 6) & 0x3) << 5);
        w |= 0b00;
        return w;
    }

    // Quadrant 0: c.sw rs2', uimm(rs1')  (funct3=110)
    private static uint AssembleCSw(Instruction ins)
    {
        if (ins.Operands.Length != 2) throw new ArgumentException("c.sw requires rs2', uimm(rs1')");
        int rs2 = (int)ParseGprPrime(ins.Operands[0]);
        var (rs1p, uimm) = ParseMemPrime(ins.Operands[1]);
        ushort w = 0;
        w |= (ushort)(0b110 << 13);
        w |= (ushort)(rs2 << 2);     // rs2' in 4..2
        w |= (ushort)(rs1p << 7);    // rs1' in 9..7
        w |= (ushort)(((uimm >> 2) & 0x1) << 6);
        w |= (ushort)(((uimm >> 3) & 0x7) << 10);
        w |= (ushort)(((uimm >> 6) & 0x1) << 5);
        w |= 0b00;
        return w;
    }

    // Quadrant 0: c.addi4spn rd', nzuimm  (funct3=000)
    private static uint AssembleCAddi4Spn(Instruction ins)
    {
        // Accept either: c.addi4spn rd', imm  or  c.addi4spn rd', sp, imm
        if (ins.Operands.Length is < 2 or > 3) throw new ArgumentException("c.addi4spn requires rd', imm  (optionally 'rd', sp, imm)");
        int rdPrime = (int)ParseGprPrime(ins.Operands[0]);
        int uimm = ParseImm(ins.Operands[^1]);
        if (uimm == 0) throw new ArgumentOutOfRangeException("c.addi4spn imm must be non-zero");
        ushort w = 0;
        w |= (ushort)(0b000 << 13);
        // Map imm to ins bits like Decoder's c.addi4spn display
        w |= (ushort)(((uimm >> 6) & 0xF) << 7);  // ins[10:7]
        w |= (ushort)(((uimm >> 4) & 0x3) << 11); // ins[12:11]
        w |= (ushort)(((uimm >> 3) & 0x1) << 5);  // ins[5]
        w |= (ushort)(((uimm >> 2) & 0x1) << 6);  // ins[6]
        w |= (ushort)(rdPrime << 2);
        w |= 0b00;
        return w;
    }

    // Quadrant 2: c.lwsp rd, uimm(sp) (funct3=010)
    private static uint AssembleCLwSp(Instruction ins)
    {
        if (ins.Operands.Length != 2) throw new ArgumentException("c.lwsp requires rd, uimm(sp)");
        int rd = (int)ParseGpr(ins.Operands[0]);
        if (rd == 0) throw new ArgumentException("c.lwsp rd cannot be x0");
        int uimm = ParseMemSp(ins.Operands[1]);
        ushort w = 0;
        w |= (ushort)(0b010 << 13);
        w |= (ushort)(rd << 7);
        // imm mapping: imm[5]->bit12, imm[4:2]->bits[6:4], imm[7:6]->bits[3:2]
        w |= (ushort)(((uimm >> 5) & 0x1) << 12);
        w |= (ushort)(((uimm >> 2) & 0x7) << 4);
        w |= (ushort)(((uimm >> 6) & 0x3) << 2);
        w |= 0b10; // quadrant 2
        return w;
    }

    // Quadrant 2: c.flwsp rd, uimm(sp) (funct3=010) - like clwsp but destination is FPR
    private static uint AssembleCFlwSp(Instruction ins)
    {
        if (ins.Operands.Length != 2) throw new ArgumentException("c.flwsp requires rd, uimm(sp)");
        uint fd = ParseFpr(ins.Operands[0]);
        if (fd == 0) throw new ArgumentException("c.flwsp fd cannot be f0/invalid");
        int uimm = ParseMemSp(ins.Operands[1]);
        ushort w = 0;
        w |= (ushort)(0b010 << 13);
        w |= (ushort)((fd & 0x1F) << 7);
        w |= (ushort)(((uimm >> 5) & 0x1) << 12);
        w |= (ushort)(((uimm >> 2) & 0x7) << 4);
        w |= (ushort)(((uimm >> 6) & 0x3) << 2);
        w |= 0b10; // quadrant 2
        return w;
    }

    // Quadrant 2: c.fswsp rs2, uimm(sp) (funct3=110) - like c.swsp but operand is FPR
    private static uint AssembleCFswSp(Instruction ins)
    {
        if (ins.Operands.Length != 2) throw new ArgumentException("c.fswsp requires rs2, uimm(sp)");
        uint fs2 = ParseFpr(ins.Operands[0]);
        int uimm = ParseMemSp(ins.Operands[1]);
        ushort w = 0;
        w |= (ushort)(0b110 << 13);
        w |= (ushort)((fs2 & 0x1F) << 2);
        w |= (ushort)(((uimm >> 2) & 0xF) << 9);
        w |= (ushort)(((uimm >> 6) & 0x3) << 7);
        w |= 0b10; // quadrant 2
        return w;
    }

    // FPR parsing helpers (support names like f0..f31 and aliases ft0/fs0 etc.)
    private static uint ParseFpr(string s)
    {
        s = s.ToLower();
        if (s.StartsWith("f") && uint.TryParse(s[1..], out var n)) { if (n>31) throw new ArgumentException(); return n; }
        return s switch {
            "ft0"=>0,"ft1"=>1,"ft2"=>2,"ft3"=>3,"ft4"=>4,"ft5"=>5,"ft6"=>6,"ft7"=>7,
            "fs0"=>8,"fs1"=>9,
            "fa0"=>10,"fa1"=>11,"fa2"=>12,"fa3"=>13,"fa4"=>14,"fa5"=>15,
            "fa6"=>16,"fa7"=>17,
            "fs2"=>18,"fs3"=>19,"fs4"=>20,"fs5"=>21,"fs6"=>22,"fs7"=>23,
            "fs8"=>24,"fs9"=>25,"fs10"=>26,"fs11"=>27,
            "ft8"=>28,"ft9"=>29,"ft10"=>30,"ft11"=>31,
            _=>throw new ArgumentException($"Unknown fpr {s}")};
    }

    private static uint ParseFprPrime(string reg)
    {
        uint r = ParseFpr(reg);
        if (r < 8 || r > 15) throw new ArgumentException("RVC prime fregs must be fs0..fs7 (f8..f15)");
        return r - 8;
    }

    // Quadrant 2: c.swsp rs2, uimm(sp) (funct3=110)
    private static uint AssembleCSwSp(Instruction ins)
    {
        if (ins.Operands.Length != 2) throw new ArgumentException("c.swsp requires rs2, uimm(sp)");
        int rs2 = (int)ParseGpr(ins.Operands[0]);
        int uimm = ParseMemSp(ins.Operands[1]);
        ushort w = 0;
        w |= (ushort)(0b110 << 13);
        w |= (ushort)(rs2 << 2);
        // imm mapping: imm[5:2]->bits[12:9], imm[7:6]->bits[8:7]
        w |= (ushort)(((uimm >> 2) & 0xF) << 9);
        w |= (ushort)(((uimm >> 6) & 0x3) << 7);
        w |= 0b10; // quadrant 2
        return w;
    }

    // Quadrant 2: c.ldsp rd, uimm(sp) (funct3=011)
    private static uint AssembleCLdSp(Instruction ins)
    {
        if (ins.Operands.Length != 2) throw new ArgumentException("c.ldsp requires rd, uimm(sp)");
        int rd = (int)ParseGpr(ins.Operands[0]);
        if (rd == 0) throw new ArgumentException("c.ldsp rd cannot be x0");
        int uimm = ParseMemSp(ins.Operands[1]);
        ushort w = 0;
        w |= (ushort)(0b011 << 13);
        w |= (ushort)(rd << 7);
        // imm mapping: imm[5]->bit12, imm[4:3]->bits[6:5], imm[8:6]->bits[4:2]
        w |= (ushort)(((uimm >> 5) & 0x1) << 12);
        w |= (ushort)(((uimm >> 3) & 0x3) << 5);
        w |= (ushort)(((uimm >> 6) & 0x7) << 2);
        w |= 0b10; // quadrant 2
        return w;
    }

    // Quadrant 2: c.fldsp rd, uimm(sp) (funct3=011) - double-precision load into FPR
    private static uint AssembleCFldSp(Instruction ins)
    {
        if (ins.Operands.Length != 2) throw new ArgumentException("c.fldsp requires rd, uimm(sp)");
        uint fd = ParseFpr(ins.Operands[0]);
        if (fd == 0) throw new ArgumentException("c.fldsp fd cannot be f0/invalid");
        int uimm = ParseMemSp(ins.Operands[1]);
        ushort w = 0;
        w |= (ushort)(0b011 << 13);
        w |= (ushort)((fd & 0x1F) << 7);
        w |= (ushort)(((uimm >> 5) & 0x1) << 12);
        w |= (ushort)(((uimm >> 3) & 0x3) << 5);
        w |= (ushort)(((uimm >> 6) & 0x7) << 2);
        w |= 0b10; // quadrant 2
        return w;
    }

    // Quadrant 2: c.fsdsp rs2, uimm(sp) (funct3=111) - double-precision store from FPR
    private static uint AssembleCFsdSp(Instruction ins)
    {
        if (ins.Operands.Length != 2) throw new ArgumentException("c.fsdsp requires rs2, uimm(sp)");
        uint fs2 = ParseFpr(ins.Operands[0]);
        int uimm = ParseMemSp(ins.Operands[1]);
        ushort w = 0;
        w |= (ushort)(0b111 << 13);
        w |= (ushort)((fs2 & 0x1F) << 2);
        w |= (ushort)(((uimm >> 3) & 0x7) << 10);
        w |= (ushort)(((uimm >> 6) & 0x7) << 7);
        w |= 0b10; // quadrant 2
        return w;
    }

    // Quadrant 2: c.sdsp rs2, uimm(sp) (funct3=111)
    private static uint AssembleCSdSp(Instruction ins)
    {
        if (ins.Operands.Length != 2) throw new ArgumentException("c.sdsp requires rs2, uimm(sp)");
        int rs2 = (int)ParseGpr(ins.Operands[0]);
        int uimm = ParseMemSp(ins.Operands[1]);
        ushort w = 0;
        w |= (ushort)(0b111 << 13);
        w |= (ushort)(rs2 << 2);
        // imm mapping: imm[5:3]->bits[12:10], imm[8:6]->bits[9:7]
        w |= (ushort)(((uimm >> 3) & 0x7) << 10);
        w |= (ushort)(((uimm >> 6) & 0x7) << 7);
        w |= 0b10; // quadrant 2
        return w;
    }

    // Quadrant 1: c.addw / c.subw (RV64C) (funct3=100, bits[11:10]=11, bit12=1)
    private static uint AssembleCArithW(Instruction ins)
    {
        if (ins.Operands.Length != 2) throw new ArgumentException("c.addw/c.subw requires rd', rs2'");
        int rdPrime = (int)ParseGprPrime(ins.Operands[0]);
        int rs2Prime = (int)ParseGprPrime(ins.Operands[1]);
        ushort w = 0;
        w |= (ushort)(0b100 << 13);
        w |= (ushort)(rdPrime << 7);
        w |= (ushort)(rs2Prime << 2);
        // bits [11:10] = 0b11
        w |= (ushort)(0b11 << 10);
        // bit12 = 1 indicates *w variants
        w |= (1 << 12);
        // op select in bits [6:5]: 00=subw, 01=addw
        int op = ins.Mnemonic.ToLower() == "c.addw" ? 0b01 : 0b00;
        w |= (ushort)(op << 5);
        w |= 0b01; // quadrant 1
        return w;
    }

    // Helpers
    private static ushort EncodeImm6(int imm)
    {
        // bits [6:2] = imm[4:0], bit 12 = imm[5]
        ushort w = 0;
        w |= (ushort)((imm & 0x1F) << 2);
        w |= (ushort)(((imm >> 5) & 0x1) << 12);
        return w;
    }

    private static (int rs1Prime, int uimm) ParseMemPrime(string s)
    {
        s = s.Replace(" ", string.Empty);
        var m = Regex.Match(s, @"(.+)\((.+)\)");
        if (!m.Success) throw new ArgumentException($"Invalid mem operand: {s}");
        int uimm = ParseImm(m.Groups[1].Value);
        int rs1 = (int)ParseGpr(m.Groups[2].Value);
        int rs1p = rs1 - 8;
        if (rs1p < 0 || rs1p > 7) throw new ArgumentException("RVC prime regs must be x8..x15");
        return (rs1p, uimm);
    }

    private static uint ParseGprPrime(string reg)
    {
        uint r = ParseGpr(reg);
        if (r < 8 || r > 15) throw new ArgumentException("RVC prime regs must be x8..x15");
        return r - 8;
    }

    private static int ParseMemSp(string s)
    {
        s = s.Replace(" ", string.Empty);
        var m = Regex.Match(s, @"(.+)\((.+)\)");
        if (!m.Success) throw new ArgumentException($"Invalid mem operand: {s}");
        int uimm = ParseImm(m.Groups[1].Value);
        string baseReg = m.Groups[2].Value.ToLower();
        if (!(baseReg == "x2" || baseReg == "sp")) throw new ArgumentException("base must be sp/x2 for *sp forms");
        return uimm;
    }

    private static uint ParseGpr(string reg)
    {
        reg = reg.ToLower();
        if (reg.StartsWith("x") && uint.TryParse(reg[1..], out var n)) { if (n > 31) throw new ArgumentException(); return n; }
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
            _ => throw new ArgumentException($"Unknown reg {reg}")
        };
    }

    private static int ParseImm(string s)
    {
        s = s.ToLower();
        if (s.StartsWith("0x")) return Convert.ToInt32(s, 16);
        if (int.TryParse(s, out var v)) return v;
        throw new ArgumentException($"Invalid immediate: {s}");
    }
}
