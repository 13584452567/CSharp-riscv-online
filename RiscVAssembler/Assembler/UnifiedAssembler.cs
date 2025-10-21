// RiscVAssembler/Assembler/UnifiedAssembler.cs
using RiscVAssembler.RiscV;

namespace RiscVAssembler.Assembler;

public class UnifiedAssembler
{
    private readonly Dictionary<string, Func<Instruction, IEnumerable<uint>>> _handlers = new(StringComparer.OrdinalIgnoreCase);

    public UnifiedAssembler()
    {
        Register(new Rv32iAssembler());
        Register(new Rv64iAssembler());
        Register(new RvzicsrAssembler());
        Register(new RvaAssembler());
        Register(new RvfAssembler());
        Register(new RvcAssembler());
        Register(new RvmAssembler());
        Register(new RvvAssembler());
    }

    public void Register(IRiscVAssemblerModule module)
    {
        foreach (var kv in module.GetHandlers())
        {
            _handlers[kv.Key] = kv.Value;
            // Also register common atomic suffix variants so mnemonics like 'lr.d.aq' or 'amoswap.d.aqrl' are supported
            try
            {
                _handlers[$"{kv.Key}.aq"] = kv.Value;
                _handlers[$"{kv.Key}.rl"] = kv.Value;
                _handlers[$"{kv.Key}.aqrl"] = kv.Value;
            }
            catch { /* ignore duplicates */ }
        }
    }

    public IEnumerable<uint> Assemble(string code)
    {
        // Two-pass assembly:
        // Pass 1: collect labels and estimate addresses (PC in bytes)
        // Pass 2: assemble with symbol resolution
        var rawLines = code.Split(new[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(l => l.Trim())
                          .Where(l => !string.IsNullOrWhiteSpace(l))
                          .ToList();

        // First pass: build symbol table
        AssemblySymbols.Symbols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int pc = 0; // byte address
        var multiWordPseudos = new[] { "li", "la", "call", "tail", "push", "pop", "pushm", "popm" };

        foreach (var line in rawLines)
        {
            // label definition: ends with ':'
            if (line.EndsWith(":"))
            {
                var label = line[..^1].Trim();
                if (!string.IsNullOrWhiteSpace(label)) AssemblySymbols.Symbols[label] = pc;
                continue;
            }

            var ins = Instruction.Parse(line);
            string lookup = ins.Mnemonic.ToLower();
            // atomic suffix strip
            if (lookup.EndsWith(".aq")) lookup = lookup[..^3];
            if (lookup.EndsWith(".rl")) lookup = lookup[..^3];

            // Estimate length (in bytes) conservatively for pseudos that may expand
            if (multiWordPseudos.Contains(lookup))
            {
                // heuristics: if immediate operand is numeric and fits 12-bit -> 4 bytes, else assume 8 bytes
                bool likelyOneWord = false;
                if ((lookup == "li" || lookup == "la" || lookup == "call" || lookup == "tail") && ins.Operands.Length >= 1)
                {
                    var op = ins.Operands.Last();
                    if (int.TryParse(op, out var v) || (op.StartsWith("0x") && int.TryParse(op.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out v)))
                    {
                        if (v >= -2048 && v <= 2047) likelyOneWord = true;
                    }
                }
                if (likelyOneWord) pc += 4; else pc += 8;
            }
            else
            {
                // default assume single 32-bit word
                pc += 4;
            }
        }

        // Second pass: assemble with symbol resolution
        AssemblySymbols.CurrentPc = 0;
        AssemblySymbols.TreatLabelAsRelative = false;
        foreach (var line in rawLines)
        {
            if (line.EndsWith(":"))
            {
                // label only
                continue;
            }

            var insn = Instruction.Parse(line);
            // Allow suffixes like .aq and .rl on atomic mnemonics by stripping them for lookup
            string lookup = insn.Mnemonic.ToLower();
            bool stripped = true;
            while (stripped)
            {
                stripped = false;
                if (lookup.EndsWith(".aq")) { lookup = lookup[..^3]; stripped = true; }
                if (lookup.EndsWith(".rl")) { lookup = lookup[..^3]; stripped = true; }
            }
            if (_handlers.TryGetValue(lookup, out var fn))
            {
                // before calling handler, set current PC (in bytes)
                AssemblySymbols.CurrentPc = AssemblySymbols.CurrentPc; // keep current
                var emitted = fn(insn).ToArray();
                foreach (var word in emitted)
                    yield return word;
                AssemblySymbols.CurrentPc += emitted.Length * 4;
            }
            else
                throw new NotSupportedException($"Instruction '{insn.Mnemonic}' not supported.");
        }
    }
}
