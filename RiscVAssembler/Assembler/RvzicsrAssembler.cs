// RiscVAssembler/Assembler/RvzicsrAssembler.cs
using RiscVAssembler.RiscV;

namespace RiscVAssembler.Assembler;

public class RvzicsrAssembler : IRiscVAssemblerModule
{
    private readonly Dictionary<string, Func<Instruction, IEnumerable<uint>>> _handlers;

    public RvzicsrAssembler()
    {
        _handlers = new(StringComparer.OrdinalIgnoreCase)
        {
            // full forms
            { "csrrw",  i => new[] { AssembleCsr(i, funct3:0b001) } },
            { "csrrs",  i => new[] { AssembleCsr(i, funct3:0b010) } },
            { "csrrc",  i => new[] { AssembleCsr(i, funct3:0b011) } },
            { "csrrwi", i => new[] { AssembleCsrImm(i, funct3:0b101) } },
            { "csrrsi", i => new[] { AssembleCsrImm(i, funct3:0b110) } },
            { "csrrci", i => new[] { AssembleCsrImm(i, funct3:0b111) } },

            // Common assembler shorthand: csrw csr, rs  -> csrrw x0, csr, rs
            //                           csrs csr, rs  -> csrrs x0, csr, rs
            //                           csrc csr, rs  -> csrrc x0, csr, rs
            { "csrw",  i => new[] { AssembleCsr(ExpandTwoOperandCsr(i, "x0"), funct3:0b001) } },
            { "csrs",  i => new[] { AssembleCsr(ExpandTwoOperandCsr(i, "x0"), funct3:0b010) } },
            { "csrc",  i => new[] { AssembleCsr(ExpandTwoOperandCsr(i, "x0"), funct3:0b011) } },

            // CSR read/write pseudos from the assembler manual
            { "csrr",   i => new[] { AssembleCsrr(i) } },
            { "csrwi",  i => new[] { AssembleCsrImmShorthand(i, 0b101, mask:0x1F) } },
            { "csrsi",  i => new[] { AssembleCsrImmShorthand(i, 0b110, mask:0x1F) } },
            { "csrci",  i => new[] { AssembleCsrImmShorthand(i, 0b111, mask:0x1F) } },

            { "rdcycle",    i => new[] { AssembleReadCsrPseudo(i, "cycle") } },
            { "rdcycleh",   i => new[] { AssembleReadCsrPseudo(i, "cycleh") } },
            { "rdtime",     i => new[] { AssembleReadCsrPseudo(i, "time") } },
            { "rdtimeh",    i => new[] { AssembleReadCsrPseudo(i, "timeh") } },
            { "rdinstret",  i => new[] { AssembleReadCsrPseudo(i, "instret") } },
            { "rdinstreth", i => new[] { AssembleReadCsrPseudo(i, "instreth") } },

            { "frcsr",   i => new[] { AssembleReadCsrPseudo(i, "fcsr") } },
            { "frflags", i => new[] { AssembleReadCsrPseudo(i, "fflags") } },
            { "frrm",    i => new[] { AssembleReadCsrPseudo(i, "frm") } },
            { "fscsr",   i => new[] { AssembleCsrWritePseudo(i, "fcsr", 0b001) } },
            { "fsflags", i => new[] { AssembleCsrWritePseudo(i, "fflags", 0b001) } },
            { "fsrm",    i => new[] { AssembleCsrWritePseudo(i, "frm", 0b001) } },
            { "fsflagsi", i => new[] { AssembleCsrWriteImmPseudo(i, "fflags", 0b101, mask:0x1F) } },
            { "fsrmi",    i => new[] { AssembleCsrWriteImmPseudo(i, "frm", 0b101, mask:0x07) } },

            { "ecall",  i => new[] { InstructionBuilder.BuildIType(Opcodes.SYSTEM, 0b000, 0, 0, 0) } },
            { "ebreak", i => new[] { InstructionBuilder.BuildIType(Opcodes.SYSTEM, 0b000, 0, 0, 1) } },
            // Privileged / system fence / wait-for-interrupt
            // Encodings use the SYSTEM opcode with specific imm[11:0] values encoded in bits 31..20
            // Standard encodings (from RISC-V privileged spec):
            //  mret: 0x30200073, sret: 0x10200073, uret: 0x00200073, wfi: 0x10500073
            //  sfence.vma: 0x12000073, sfence.vm: 0x12200073
            { "mret",    i => new[] { 0x30200073u } },
            { "sret",    i => new[] { 0x10200073u } },
            { "uret",    i => new[] { 0x00200073u } },
            { "wfi",     i => new[] { 0x10500073u } },
            { "sfence.vma", i => new[] { 0x12000073u } },
            { "sfence.vm",  i => new[] { 0x12200073u } },
        };
    }

    public IReadOnlyDictionary<string, Func<Instruction, IEnumerable<uint>>> GetHandlers() => _handlers;

    private uint AssembleCsr(Instruction i, uint funct3)
    {
        if (i.Operands.Length != 3) throw new ArgumentException("CSR requires rd, csr, rs1");
        uint rd = ParseGpr(i.Operands[0]);
        int csr = ParseCsr(i.Operands[1]);
        uint rs1 = ParseGpr(i.Operands[2]);
        return Opcodes.SYSTEM | ((uint)csr << 20) | (rs1 << 15) | (funct3 << 12) | (rd << 7);
    }

    // Helper: take an Instruction with two operands (csr, rs) and return a new
    // Instruction with three operands (rd, csr, rs) where rd is the provided rdName.
    private static Instruction ExpandTwoOperandCsr(Instruction i, string rdName)
    {
        // Expected forms: operands[0] = csr, operands[1] = rs
        if (i.Operands.Length != 2)
            throw new ArgumentException("CSR shorthand requires 2 operands: csr, rs");
        var newOperands = new[] { rdName, i.Operands[0], i.Operands[1] };
        return new Instruction(i.Mnemonic, newOperands);
    }

    private uint AssembleCsrImm(Instruction i, uint funct3)
    {
        if (i.Operands.Length != 3) throw new ArgumentException("CSR*I requires rd, csr, zimm");
        uint rd = ParseGpr(i.Operands[0]);
        int csr = ParseCsr(i.Operands[1]);
        uint zimm = (uint)(ParseImm(i.Operands[2]) & 0x1F);
        return Opcodes.SYSTEM | ((uint)csr << 20) | (zimm << 15) | (funct3 << 12) | (rd << 7);
    }

    private uint AssembleCsrr(Instruction instruction)
    {
        if (instruction.Operands.Length != 2) throw new ArgumentException("csrr requires rd, csr");
        var expanded = new Instruction("csrrs", new[] { instruction.Operands[0], instruction.Operands[1], "x0" });
        return AssembleCsr(expanded, 0b010);
    }

    private uint AssembleCsrImmShorthand(Instruction instruction, uint funct3, int mask)
    {
        if (instruction.Operands.Length != 2) throw new ArgumentException("CSR immediate pseudo requires csr, zimm");
        string csrToken = instruction.Operands[0];
        string imm = NormalizeZimm(instruction.Operands[1], mask);
        var expanded = new Instruction("csrrwi", new[] { "x0", csrToken, imm });
        return AssembleCsrImm(expanded, funct3);
    }

    private uint AssembleReadCsrPseudo(Instruction instruction, string csrName)
    {
        if (instruction.Operands.Length != 1) throw new ArgumentException($"{instruction.Mnemonic} requires rd");
        var expanded = new Instruction("csrrs", new[] { instruction.Operands[0], csrName, "x0" });
        return AssembleCsr(expanded, 0b010);
    }

    private uint AssembleCsrWritePseudo(Instruction instruction, string csrName, uint funct3)
    {
        string rd;
        string rs;
        if (instruction.Operands.Length == 2)
        {
            rd = instruction.Operands[0];
            rs = instruction.Operands[1];
        }
        else if (instruction.Operands.Length == 1)
        {
            rd = "x0";
            rs = instruction.Operands[0];
        }
        else
        {
            throw new ArgumentException($"{instruction.Mnemonic} requires rd?, rs");
        }
        var expanded = new Instruction("csrrw", new[] { rd, csrName, rs });
        return AssembleCsr(expanded, funct3);
    }

    private uint AssembleCsrWriteImmPseudo(Instruction instruction, string csrName, uint funct3, int mask)
    {
        string rd;
        string immToken;
        if (instruction.Operands.Length == 2)
        {
            rd = instruction.Operands[0];
            immToken = NormalizeZimm(instruction.Operands[1], mask);
        }
        else if (instruction.Operands.Length == 1)
        {
            rd = "x0";
            immToken = NormalizeZimm(instruction.Operands[0], mask);
        }
        else
        {
            throw new ArgumentException($"{instruction.Mnemonic} requires rd?, imm");
        }
        var expanded = new Instruction("csrrwi", new[] { rd, csrName, immToken });
        return AssembleCsrImm(expanded, funct3);
    }

    private static string NormalizeZimm(string token, int mask)
    {
        int value = ParseImm(token);
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(token), "Immediate must be non-negative");
        value &= mask;
        return value.ToString();
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
