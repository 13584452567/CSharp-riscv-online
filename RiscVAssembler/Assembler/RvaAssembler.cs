// RiscVAssembler/Assembler/RvaAssembler.cs
using RiscVAssembler.RiscV;

namespace RiscVAssembler.Assembler;

public class RvaAssembler : IRiscVAssemblerModule
{
    private readonly Dictionary<string, Func<Instruction, IEnumerable<uint>>> _handlers;

    public RvaAssembler()
    {
    _handlers = new(StringComparer.OrdinalIgnoreCase)
        {
            // lr/sc
            { "lr.w",  i => new[] { AssembleAmoWithFlags(i, 0b00010, 0b010, rs2Zero:true) } },
            { "sc.w",  i => new[] { AssembleAmoWithFlags(i, 0b00011, 0b010) } },
            { "lr.d",  i => new[] { AssembleAmoWithFlags(i, 0b00010, 0b011, rs2Zero:true) } },
            { "sc.d",  i => new[] { AssembleAmoWithFlags(i, 0b00011, 0b011) } },

            // amoxxx.w
            { "amoswap.w", i => new[] { AssembleAmoWithFlags(i, 0b00001, 0b010) } },
            { "amoadd.w",  i => new[] { AssembleAmo(i, 0b00000, false, false, 0b010) } },
            { "amoxor.w",  i => new[] { AssembleAmo(i, 0b00100, false, false, 0b010) } },
            { "amoand.w",  i => new[] { AssembleAmo(i, 0b01100, false, false, 0b010) } },
            { "amoor.w",   i => new[] { AssembleAmo(i, 0b01000, false, false, 0b010) } },
            { "amomin.w",  i => new[] { AssembleAmo(i, 0b10000, false, false, 0b010) } },
            { "amomax.w",  i => new[] { AssembleAmo(i, 0b10100, false, false, 0b010) } },
            { "amominu.w", i => new[] { AssembleAmo(i, 0b11000, false, false, 0b010) } },
            { "amomaxu.w", i => new[] { AssembleAmo(i, 0b11100, false, false, 0b010) } },

            // amoxxx.d
            { "amoswap.d", i => new[] { AssembleAmoWithFlags(i, 0b00001, 0b011) } },
            { "amoadd.d",  i => new[] { AssembleAmoWithFlags(i, 0b00000, 0b011) } },
            { "amoxor.d",  i => new[] { AssembleAmoWithFlags(i, 0b00100, 0b011) } },
            { "amoand.d",  i => new[] { AssembleAmoWithFlags(i, 0b01100, 0b011) } },
            { "amoor.d",   i => new[] { AssembleAmoWithFlags(i, 0b01000, 0b011) } },
            { "amomin.d",  i => new[] { AssembleAmoWithFlags(i, 0b10000, 0b011) } },
            { "amomax.d",  i => new[] { AssembleAmoWithFlags(i, 0b10100, 0b011) } },
            { "amominu.d", i => new[] { AssembleAmoWithFlags(i, 0b11000, 0b011) } },
            { "amomaxu.d", i => new[] { AssembleAmoWithFlags(i, 0b11100, 0b011) } },
        };
    }

    public IReadOnlyDictionary<string, Func<Instruction, IEnumerable<uint>>> GetHandlers() => _handlers;

    private static uint AssembleAmo(Instruction i, uint funct5, bool aq, bool rl, uint funct3, bool rs2Zero=false)
    {
        // Support syntax:
        // - amoxxx rd, rs1, rs2
        // - lr.*   rd, (rs1)   or lr.* rd, rs1
        if (!rs2Zero && i.Operands.Length != 3)
            throw new ArgumentException("AMO requires rd, rs1, rs2");
        uint rd = ParseGpr(i.Operands[0]);

        uint rs1; uint rs2;
        if (rs2Zero)
        {
            if (i.Operands.Length < 2) throw new ArgumentException("lr.* requires rd, (rs1) or rd, rs1");
            var op = i.Operands[1].Trim();
            if (op.StartsWith("(") && op.EndsWith(")"))
            {
                rs1 = ParseGpr(op[1..^1]);
            }
            else
            {
                rs1 = ParseGpr(op);
            }
            rs2 = 0;
        }
        else
        {
            rs1 = ParseGpr(i.Operands[1]);
            rs2 = ParseGpr(i.Operands[2]);
        }
        return InstructionBuilder.BuildAmo(funct5, aq, rl, funct3, rd, rs1, rs2);
    }

    // Wrap AssembleAmo and detect .aq/.rl suffixes in the mnemonic
    private static uint AssembleAmoWithFlags(Instruction i, uint funct5, uint funct3, bool rs2Zero=false)
    {
        var mnemonic = i.Mnemonic.ToLower();
        bool aq = mnemonic.EndsWith(".aq");
        bool rl = mnemonic.EndsWith(".rl");
        bool aqrl = mnemonic.EndsWith(".aqrl") || mnemonic.EndsWith(".rlaq");
        if (aqrl) { aq = true; rl = true; }
        // Clean mnemonic (not strictly necessary here but kept for clarity)
        return AssembleAmo(i, funct5, aq, rl, funct3, rs2Zero);
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
