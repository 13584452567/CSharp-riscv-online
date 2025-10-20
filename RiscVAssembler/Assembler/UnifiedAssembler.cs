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
        var lines = code.Split(new[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrWhiteSpace(l));
        foreach (var line in lines)
        {
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
                foreach (var word in fn(insn))
                    yield return word;
            }
            else
                throw new NotSupportedException($"Instruction '{insn.Mnemonic}' not supported.");
        }
    }
}
