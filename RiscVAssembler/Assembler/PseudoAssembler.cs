using RiscVAssembler.RiscV;
using System.Collections.Generic;
using System;

namespace RiscVAssembler.Assembler
{
    public class PseudoAssembler : IRiscVAssemblerModule
    {
        public IReadOnlyDictionary<string, Func<Instruction, IEnumerable<uint>>> GetHandlers()
        {
            return new Dictionary<string, Func<Instruction, IEnumerable<uint>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["nop"] = AssembleNop,
                ["li"] = AssembleLi,
                ["mv"] = AssembleMv,
                ["not"] = AssembleNot,
                ["neg"] = AssembleNeg,
                ["seqz"] = AssembleSeqz,
                ["snez"] = AssembleSnez,
                ["sltz"] = AssembleSltz,
                ["sgtz"] = AssembleSgtz,
                ["beqz"] = AssembleBeqz,
                ["bnez"] = AssembleBnez,
                ["blez"] = AssembleBlez,
                ["bgez"] = AssembleBgez,
                ["bltz"] = AssembleBltz,
                ["bgtz"] = AssembleBgtz,
                ["j"] = AssembleJ,
                ["jal"] = AssembleJal,
                ["jr"] = AssembleJr,
                ["jalr"] = AssembleJalr,
                ["ret"] = AssembleRet,
                ["call"] = AssembleCall,
                ["tail"] = AssembleTail,
            };
        }

        private IEnumerable<uint> AssembleNop(Instruction instruction) => new Rv32iAssembler().GetHandlers()["addi"](Instruction.Parse("addi x0, x0, 0"));
        private IEnumerable<uint> AssembleLi(Instruction instruction)
        {
            // This is a simplified version. A full implementation would handle large immediates.
            return new Rv32iAssembler().GetHandlers()["addi"](Instruction.Parse($"addi {instruction.Operands[0]}, x0, {instruction.Operands[1]}"));
        }
        private IEnumerable<uint> AssembleMv(Instruction instruction) => new Rv32iAssembler().GetHandlers()["addi"](Instruction.Parse($"addi {instruction.Operands[0]}, {instruction.Operands[1]}, 0"));
        private IEnumerable<uint> AssembleNot(Instruction instruction) => new Rv32iAssembler().GetHandlers()["xori"](Instruction.Parse($"xori {instruction.Operands[0]}, {instruction.Operands[1]}, -1"));
        private IEnumerable<uint> AssembleNeg(Instruction instruction) => new Rv32iAssembler().GetHandlers()["sub"](Instruction.Parse($"sub {instruction.Operands[0]}, x0, {instruction.Operands[1]}"));
        private IEnumerable<uint> AssembleSeqz(Instruction instruction) => new Rv32iAssembler().GetHandlers()["sltiu"](Instruction.Parse($"sltiu {instruction.Operands[0]}, {instruction.Operands[1]}, 1"));
        private IEnumerable<uint> AssembleSnez(Instruction instruction) => new Rv32iAssembler().GetHandlers()["sltu"](Instruction.Parse($"sltu {instruction.Operands[0]}, x0, {instruction.Operands[1]}"));
        private IEnumerable<uint> AssembleSltz(Instruction instruction) => new Rv32iAssembler().GetHandlers()["slt"](Instruction.Parse($"slt {instruction.Operands[0]}, {instruction.Operands[1]}, x0"));
        private IEnumerable<uint> AssembleSgtz(Instruction instruction) => new Rv32iAssembler().GetHandlers()["slt"](Instruction.Parse($"slt {instruction.Operands[0]}, x0, {instruction.Operands[1]}"));
        private IEnumerable<uint> AssembleBeqz(Instruction instruction) => new Rv32iAssembler().GetHandlers()["beq"](Instruction.Parse($"beq {instruction.Operands[0]}, x0, {instruction.Operands[1]}"));
        private IEnumerable<uint> AssembleBnez(Instruction instruction) => new Rv32iAssembler().GetHandlers()["bne"](Instruction.Parse($"bne {instruction.Operands[0]}, x0, {instruction.Operands[1]}"));
        private IEnumerable<uint> AssembleBlez(Instruction instruction) => new Rv32iAssembler().GetHandlers()["bge"](Instruction.Parse($"bge x0, {instruction.Operands[0]}, {instruction.Operands[1]}"));
        private IEnumerable<uint> AssembleBgez(Instruction instruction) => new Rv32iAssembler().GetHandlers()["bge"](Instruction.Parse($"bge {instruction.Operands[0]}, x0, {instruction.Operands[1]}"));
        private IEnumerable<uint> AssembleBltz(Instruction instruction) => new Rv32iAssembler().GetHandlers()["blt"](Instruction.Parse($"blt {instruction.Operands[0]}, x0, {instruction.Operands[1]}"));
        private IEnumerable<uint> AssembleBgtz(Instruction instruction) => new Rv32iAssembler().GetHandlers()["blt"](Instruction.Parse($"blt x0, {instruction.Operands[0]}, {instruction.Operands[1]}"));
        private IEnumerable<uint> AssembleJ(Instruction instruction) => new Rv32iAssembler().GetHandlers()["jal"](Instruction.Parse($"jal x0, {instruction.Operands[0]}"));
        private IEnumerable<uint> AssembleJal(Instruction instruction) => new Rv32iAssembler().GetHandlers()["jal"](Instruction.Parse($"jal x1, {instruction.Operands[0]}"));
        private IEnumerable<uint> AssembleJr(Instruction instruction) => new Rv32iAssembler().GetHandlers()["jalr"](Instruction.Parse($"jalr x0, {instruction.Operands[0]}, 0"));
        private IEnumerable<uint> AssembleJalr(Instruction instruction) => new Rv32iAssembler().GetHandlers()["jalr"](Instruction.Parse($"jalr x1, {instruction.Operands[0]}, 0"));
        private IEnumerable<uint> AssembleRet(Instruction instruction) => new Rv32iAssembler().GetHandlers()["jalr"](Instruction.Parse("jalr x0, x1, 0"));
        private IEnumerable<uint> AssembleCall(Instruction instruction)
        {
            // Simplified but robust: materialize the sequences returned by underlying handlers
            var auipcSeq = new Rv32iAssembler().GetHandlers()["auipc"](Instruction.Parse($"auipc x1, 0"));
            foreach (var w in auipcSeq) yield return w;
            var jalrSeq = new Rv32iAssembler().GetHandlers()["jalr"](Instruction.Parse($"jalr x1, x1, 0"));
            foreach (var w in jalrSeq) yield return w;
        }
        private IEnumerable<uint> AssembleTail(Instruction instruction)
        {
            var auipcSeq = new Rv32iAssembler().GetHandlers()["auipc"](Instruction.Parse($"auipc x6, 0"));
            foreach (var w in auipcSeq) yield return w;
            var jalrSeq = new Rv32iAssembler().GetHandlers()["jalr"](Instruction.Parse($"jalr x0, x6, 0"));
            foreach (var w in jalrSeq) yield return w;
        }
    }
}
