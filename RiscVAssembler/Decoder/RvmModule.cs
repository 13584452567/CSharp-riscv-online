using RiscVAssembler.RiscV;

namespace RiscVAssembler.Decoder
{
    public class RvmModule : IDisassemblerModule
    {
        private readonly RvmDisassembler _disassembler;

        public RvmModule(Xlen xlen)
        {
            _disassembler = new RvmDisassembler(xlen);
        }

        public bool TryDisassemble(uint instruction, out string text)
        {
            return _disassembler.TryDisassemble(instruction, out text);
        }
    }
}
