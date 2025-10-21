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
            // Placeholder: RVV not yet implemented. Throw to avoid returning incorrect encodings.
            throw new NotSupportedException("RVV instructions are not yet implemented by the assembler.");
        }
    }
}
