using RiscVAssembler.RiscV;

namespace RiscVAssembler.Decoder
{
    public class RvmDisassembler
    {
        private readonly Xlen _xlen;
        public RvmDisassembler(Xlen xlen)
        {
            _xlen = xlen;
        }

        public bool TryDisassemble(uint instruction, out string text)
        {
            var opcode = instruction & 0x7Fu;
            var funct7 = (instruction >> 25) & 0x7Fu;

            if (funct7 != Funct7.MULDIV || (opcode != Opcodes.OP && opcode != Opcodes.OP_32))
            {
                text = string.Empty;
                return false;
            }

            text = DecodeRType(instruction);
            return true;
        }

        private string GetOpInstructionName(uint instruction)
        {
            var opcode = instruction & 0x7Fu;
            var funct3 = (instruction >> 12) & 0x7;

            if (opcode == Opcodes.OP_32)
            {
                return funct3 switch
                {
                    Funct3.MULW => "mulw",
                    Funct3.DIVW => "divw",
                    Funct3.DIVUW => "divuw",
                    Funct3.REMW => "remw",
                    Funct3.REMUW => "remuw",
                    _ => "unknown_op"
                };
            }

            return funct3 switch
            {
                Funct3.MUL => "mul",
                Funct3.MULH => "mulh",
                Funct3.MULHSU => "mulhsu",
                Funct3.MULHU => "mulhu",
                Funct3.DIV => "div",
                Funct3.DIVU => "divu",
                Funct3.REM => "rem",
                Funct3.REMU => "remu",
                _ => "unknown_op"
            };
        }

        private string DecodeRType(uint instruction)
        {
            var rd = (instruction >> 7) & 0x1F;
            var rs1 = (instruction >> 15) & 0x1F;
            var rs2 = (instruction >> 20) & 0x1F;

            var instructionName = GetOpInstructionName(instruction);

            if (_xlen == Xlen.X32)
            {
                if (instructionName.EndsWith("w"))
                {
                    return "unknown_op";
                }
            }

            return $"{instructionName} {RegisterUtils.RegName((int)rd)}, {RegisterUtils.RegName((int)rs1)}, {RegisterUtils.RegName((int)rs2)}";
        }
    }
}
