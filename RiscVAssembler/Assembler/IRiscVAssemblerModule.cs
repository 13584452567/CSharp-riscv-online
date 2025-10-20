// RiscVAssembler/Assembler/IRiscVAssemblerModule.cs
using RiscVAssembler.RiscV;

namespace RiscVAssembler.Assembler;

public interface IRiscVAssemblerModule
{
    // Returns handlers mapping mnemonic -> assembler function(s). Each handler may emit one or more 32-bit
    // instructions for a single assembly mnemonic (to support pseudo-instruction expansion).
    IReadOnlyDictionary<string, Func<Instruction, IEnumerable<uint>>> GetHandlers();
}
