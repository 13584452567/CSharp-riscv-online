// RiscVAssembler/Assembler/IRiscVAssemblerModule.cs
using RiscVAssembler.RiscV;

namespace RiscVAssembler.Assembler;

public interface IRiscVAssemblerModule
{
    // Returns handlers mapping mnemonic -> assembler function
    IReadOnlyDictionary<string, Func<Instruction, uint>> GetHandlers();
}
