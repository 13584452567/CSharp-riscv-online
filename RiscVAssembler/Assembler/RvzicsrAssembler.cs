// RiscVAssembler/Assembler/RvzicsrAssembler.cs
using RiscVAssembler.RiscV;

namespace RiscVAssembler.Assembler;

public class RvzicsrAssembler : IRiscVAssemblerModule
{
    private readonly Dictionary<string, Func<Instruction, uint>> _handlers;

    public RvzicsrAssembler()
    {
        _handlers = new(StringComparer.OrdinalIgnoreCase)
        {
            { "csrrw",  i => AssembleCsr(i, funct3:0b001) },
            { "csrrs",  i => AssembleCsr(i, funct3:0b010) },
            { "csrrc",  i => AssembleCsr(i, funct3:0b011) },
            { "csrrwi", i => AssembleCsrImm(i, funct3:0b101) },
            { "csrrsi", i => AssembleCsrImm(i, funct3:0b110) },
            { "csrrci", i => AssembleCsrImm(i, funct3:0b111) },
            { "ecall",  i => InstructionBuilder.BuildIType(Opcodes.SYSTEM, 0b000, 0, 0, 0) },
            { "ebreak", i => InstructionBuilder.BuildIType(Opcodes.SYSTEM, 0b000, 0, 0, 1) },
        };
    }

    public IReadOnlyDictionary<string, Func<Instruction, uint>> GetHandlers() => _handlers;

    private uint AssembleCsr(Instruction i, uint funct3)
    {
        if (i.Operands.Length != 3) throw new ArgumentException("CSR requires rd, csr, rs1");
        uint rd = ParseGpr(i.Operands[0]);
        int csr = ParseCsr(i.Operands[1]);
        uint rs1 = ParseGpr(i.Operands[2]);
        return Opcodes.SYSTEM | ((uint)csr << 20) | (rs1 << 15) | (funct3 << 12) | (rd << 7);
    }

    private uint AssembleCsrImm(Instruction i, uint funct3)
    {
        if (i.Operands.Length != 3) throw new ArgumentException("CSR*I requires rd, csr, zimm");
        uint rd = ParseGpr(i.Operands[0]);
        int csr = ParseCsr(i.Operands[1]);
        uint zimm = (uint)(ParseImm(i.Operands[2]) & 0x1F);
        return Opcodes.SYSTEM | ((uint)csr << 20) | (zimm << 15) | (funct3 << 12) | (rd << 7);
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

    private static int ParseCsr(string s)
    {
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return Convert.ToInt32(s, 16);
        if (int.TryParse(s, out var v)) return v;
        if (Csr.TryGet(s, out var addr)) return addr;
        throw new ArgumentException($"Unknown CSR: {s}");
    }

    private static int ParseImm(string s)
    {
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return Convert.ToInt32(s, 16);
        if (int.TryParse(s, out var v)) return v;
        throw new ArgumentException($"Invalid immediate: {s}");
    }
}
