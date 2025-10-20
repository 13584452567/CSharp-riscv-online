using RiscVAssembler.RiscV;

namespace RiscVAssembler.Decoder
{
    public class RvvModule : IDisassemblerModule
    {
        private readonly RvvDisassembler _disassembler = new();

        public bool TryDisassemble(uint instruction, out string text)
        {
            return _disassembler.TryDisassemble(instruction, out text);
        }
    }
}
