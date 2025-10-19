// RiscVAssembler/Decoder/IDisassemblerModule.cs
namespace RiscVAssembler.Decoder;

public interface IDisassemblerModule
{
    // Return true if this module handled the instruction and output text
    bool TryDisassemble(uint instruction, out string text);
}
