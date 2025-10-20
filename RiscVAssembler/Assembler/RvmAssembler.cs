using RiscVAssembler.RiscV;

namespace RiscVAssembler.Assembler
{
    public class RvmAssembler : IRiscVAssemblerModule
    {
        public IReadOnlyDictionary<string, Func<Instruction, IEnumerable<uint>>> GetHandlers()
        {
            return new Dictionary<string, Func<Instruction, IEnumerable<uint>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["mul"] = AssembleR,
                ["mulh"] = AssembleR,
                ["mulhsu"] = AssembleR,
                ["mulhu"] = AssembleR,
                ["div"] = AssembleR,
                ["divu"] = AssembleR,
                ["rem"] = AssembleR,
                ["remu"] = AssembleR,
                ["mulw"] = AssembleR,
                ["divw"] = AssembleR,
                ["divuw"] = AssembleR,
                ["remw"] = AssembleR,
                ["remuw"] = AssembleR,
            };
        }

        private static IEnumerable<uint> AssembleR(Instruction instruction)
        {
            var funct3 = instruction.Mnemonic.ToLower() switch
            {
                "mul" => Funct3.MUL,
                "mulh" => Funct3.MULH,
                "mulhsu" => Funct3.MULHSU,
                "mulhu" => Funct3.MULHU,
                "div" => Funct3.DIV,
                "divu" => Funct3.DIVU,
                "rem" => Funct3.REM,
                "remu" => Funct3.REMU,
                "mulw" => Funct3.MULW,
                "divw" => Funct3.DIVW,
                "divuw" => Funct3.DIVUW,
                "remw" => Funct3.REMW,
                "remuw" => Funct3.REMUW,
                _ => throw new NotSupportedException(),
            };

            yield return instruction.AssembleRType(Opcodes.OP, funct3, Funct7.MULDIV);
        }
    }
}
