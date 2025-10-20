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
            if (_handlers.TryGetValue(insn.Mnemonic, out var fn))
            {
                foreach (var word in fn(insn))
                    yield return word;
            }
            else
                throw new NotSupportedException($"Instruction '{insn.Mnemonic}' not supported.");
        }
    }
}
