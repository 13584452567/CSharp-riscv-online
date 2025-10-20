using RiscVAssembler.RiscV;

namespace RiscVAssembler.Assembler
{
    public class RvvAssembler : IRiscVAssemblerModule
    {
        public IReadOnlyDictionary<string, Func<Instruction, IEnumerable<uint>>> GetHandlers()
        {
            return new Dictionary<string, Func<Instruction, IEnumerable<uint>>>(StringComparer.OrdinalIgnoreCase)
            {
                // For now, just a placeholder. A full implementation would have handlers for all V instructions.
                ["vle32.v"] = AssembleV,
            };
        }

        private static IEnumerable<uint> AssembleV(Instruction instruction)
        {
            // This is a placeholder. A real implementation would parse operands and build the instruction word.
            // For example, for vle32.v vd, (rs1), vm
            // This is a VL-format instruction.
            // For simplicity, we'll just return a fixed value.
            yield return 0x00257557; // vle32.v v10, (x10), v0.t
        }
    }
}
