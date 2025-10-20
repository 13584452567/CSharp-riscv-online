using RiscVAssembler.RiscV;

namespace RiscVAssembler.Decoder
{
    public class RvvDisassembler
    {
        public bool TryDisassemble(uint instruction, out string text)
        {
            var opcode = instruction & 0x7Fu;
            if (opcode != Opcodes.OP_V)
            {
                text = string.Empty;
                return false;
            }

            // For now, just recognize OP_V instructions as 'v' instructions
            // A full disassembler would decode funct3, funct6, etc.
            text = "v_instruction";
            return true;
        }
    }
}
