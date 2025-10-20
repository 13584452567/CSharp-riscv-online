// RiscVAssembler/Decoder/UnifiedDisassembler.cs
namespace RiscVAssembler.Decoder;

public class UnifiedDisassembler
{
    private readonly List<IDisassemblerModule> _modules = new();

    public UnifiedDisassembler(Xlen xlen = Xlen.Unknown)
    {
        _modules.Add(new Rv32iModule());
        _modules.Add(new RvcModule(xlen));
        _modules.Add(new Rv64iModule());
        _modules.Add(new RvaModule(xlen));
        _modules.Add(new RvfModule());
        _modules.Add(new RvmModule(xlen));
        _modules.Add(new RvvModule());
        _modules.Add(new RvzicsrModule());
    }

    public string Disassemble(uint instruction)
    {
        foreach (var m in _modules)
        {
            if (m.TryDisassemble(instruction, out var text))
                return text;
        }
        return "unknown";
    }
}
