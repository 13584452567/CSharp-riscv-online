// RiscVAssembler/Assembler/RvfAssembler.cs
using System.Text.RegularExpressions;
using RiscVAssembler.RiscV;

namespace RiscVAssembler.Assembler;

public class RvfAssembler : IRiscVAssemblerModule
{
    private readonly Dictionary<string, Func<Instruction, uint>> _handlers;

    public RvfAssembler()
    {
        _handlers = new(StringComparer.OrdinalIgnoreCase)
        {
            // loads/stores
            { "flw", i => AssembleLoadStore(i, isLoad:true,  funct3:0b010) },
            { "fsw", i => AssembleLoadStore(i, isLoad:false, funct3:0b010) },

            // basic fp ops
            { "fadd.s", i => AssembleFpRType(i, Fpu.FADD_S) },
            { "fsub.s", i => AssembleFpRType(i, Fpu.FSUB_S) },
            { "fmul.s", i => AssembleFpRType(i, Fpu.FMUL_S) },
            { "fdiv.s", i => AssembleFpRType(i, Fpu.FDIV_S) },
            { "fsqrt.s", i => AssembleFpRS1(i, Fpu.FSQRT_S) },

            // fused multiply-add (4 operands): rd, rs1, rs2, rs3[, rm]
            { "fmadd.s",  i => AssembleFpR4(i, Opcodes.MADD) },
            { "fmsub.s",  i => AssembleFpR4(i, Opcodes.MSUB) },
            { "fnmsub.s", i => AssembleFpR4(i, Opcodes.NMSUB) },
            { "fnmadd.s", i => AssembleFpR4(i, Opcodes.NMADD) },

            // moves and converts (simplified)
            { "fmv.x.w", i => AssembleFpMove(i, toInt:true) },
            { "fmv.w.x", i => AssembleFpMove(i, toInt:false) },
        };
    }

    public IReadOnlyDictionary<string, Func<Instruction, uint>> GetHandlers() => _handlers;

    private uint AssembleLoadStore(Instruction instruction, bool isLoad, uint funct3)
    {
        if (instruction.Operands.Length != 2) throw new ArgumentException("FLW/FSW syntax");
        if (isLoad)
        {
            uint rd = ParseFpr(instruction.Operands[0]);
            var (rs1, imm) = ParseMem(instruction.Operands[1]);
            return InstructionBuilder.BuildIType(Opcodes.LOAD_FP, funct3, rd, rs1, imm);
        }
        else
        {
            uint rs2 = ParseFpr(instruction.Operands[0]);
            var (rs1, imm) = ParseMem(instruction.Operands[1]);
            return InstructionBuilder.BuildSType(Opcodes.STORE_FP, funct3, rs1, rs2, imm);
        }
    }

    private uint AssembleFpRType(Instruction i, uint funct7)
    {
        // form: fadd.s fd, fs1, fs2[, rm]
        if (i.Operands.Length < 3) throw new ArgumentException("FP R-type requires fd, fs1, fs2[, rm]");
        uint rd = ParseFpr(i.Operands[0]);
        uint rs1 = ParseFpr(i.Operands[1]);
        uint rs2 = ParseFpr(i.Operands[2]);
        uint rm = i.Operands.Length >= 4 ? Fpu.RmFromString(i.Operands[3]) : 0;
        return InstructionBuilder.BuildFpRType(funct7, 0, rd, rs1, rs2, rm);
    }

    private uint AssembleFpRS1(Instruction i, uint funct7)
    {
        // form: fsqrt.s fd, fs1[, rm]  (rs2=0)
        if (i.Operands.Length < 2) throw new ArgumentException("FSQRT requires fd, fs1[, rm]");
        uint rd = ParseFpr(i.Operands[0]);
        uint rs1 = ParseFpr(i.Operands[1]);
        uint rm = i.Operands.Length >= 3 ? Fpu.RmFromString(i.Operands[2]) : 0;
        return InstructionBuilder.BuildFpRType(funct7, 0, rd, rs1, 0, rm);
    }

    private uint AssembleFpR4(Instruction i, uint opcode)
    {
        if (i.Operands.Length < 4) throw new ArgumentException("FMADD variants require fd, fs1, fs2, fs3[, rm]");
        uint rd = ParseFpr(i.Operands[0]);
        uint rs1 = ParseFpr(i.Operands[1]);
        uint rs2 = ParseFpr(i.Operands[2]);
        uint rs3 = ParseFpr(i.Operands[3]);
        uint rm = i.Operands.Length >= 5 ? Fpu.RmFromString(i.Operands[4]) : 0;
        return InstructionBuilder.BuildFpR4Type(opcode, rd, rs1, rs2, rs3, rm);
    }

    private uint AssembleFpMove(Instruction i, bool toInt)
    {
        // fmv.x.w rd<GPR>, rs1<FPR>
        // fmv.w.x rd<FPR>, rs1<GPR>
        if (i.Operands.Length < 2) throw new ArgumentException("FMV requires two operands");
        if (toInt)
        {
            uint rd = ParseGpr(i.Operands[0]);
            uint rs1 = ParseFpr(i.Operands[1]);
            return InstructionBuilder.BuildFpRType(Fpu.FMV_X_W, 0, rd, rs1, 0, 0);
        }
        else
        {
            uint rd = ParseFpr(i.Operands[0]);
            uint rs1 = ParseGpr(i.Operands[1]);
            return InstructionBuilder.BuildFpRType(Fpu.FMV_W_X, 0, rd, rs1, 0, 0);
        }
    }

    private static (uint rs1, int imm) ParseMem(string op)
    {
        var m = Regex.Match(op, @"(-?\d+)\(x(\d+)\)");
        if (!m.Success) throw new ArgumentException($"Invalid mem op: {op}");
        int imm = int.Parse(m.Groups[1].Value);
        uint rs1 = uint.Parse(m.Groups[2].Value);
        if (rs1>31) throw new ArgumentException();
        return (rs1, imm);
    }

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

    private static uint ParseGpr(string s)
    {
        s = s.ToLower();
        if (s.StartsWith("x") && uint.TryParse(s[1..], out var n)) { if (n>31) throw new ArgumentException(); return n; }
        return s switch {
            "zero"=>0,"ra"=>1,"sp"=>2,"gp"=>3,"tp"=>4,
            "t0"=>5,"t1"=>6,"t2"=>7,
            "s0" or "fp"=>8,"s1"=>9,
            "a0"=>10,"a1"=>11,"a2"=>12,"a3"=>13,"a4"=>14,"a5"=>15,
            "a6"=>16,"a7"=>17,
            "s2"=>18,"s3"=>19,"s4"=>20,"s5"=>21,"s6"=>22,"s7"=>23,
            "s8"=>24,"s9"=>25,"s10"=>26,"s11"=>27,
            "t3"=>28,"t4"=>29,"t5"=>30,"t6"=>31,
            _=>throw new ArgumentException($"Unknown reg {s}")};
    }
}
