using FluentAssertions;
using RiscVAssembler.Assembler;
using Xunit;

namespace RiscVAssembler.Tests
{
    public class PseudoAssemblerTests
    {
        [Theory]
        [InlineData("nop", "addi x0, x0, 0")]
        [InlineData("mv x1, x2", "addi x1, x2, 0")]
        [InlineData("not x1, x2", "xori x1, x2, -1")]
        [InlineData("neg x1, x2", "sub x1, x0, x2")]
        [InlineData("seqz x1, x2", "sltiu x1, x2, 1")]
        [InlineData("snez x1, x2", "sltu x1, x0, x2")]
        [InlineData("sltz x1, x2", "slt x1, x2, x0")]
        [InlineData("sgtz x1, x2", "slt x1, x0, x2")]
        [InlineData("beqz x1, 12", "beq x1, x0, 12")]
        [InlineData("bnez x1, 12", "bne x1, x0, 12")]
        [InlineData("blez x1, 12", "bge x0, x1, 12")]
        [InlineData("bgez x1, 12", "bge x1, x0, 12")]
        [InlineData("bltz x1, 12", "blt x1, x0, 12")]
        [InlineData("bgtz x1, 12", "blt x0, x1, 12")]
        [InlineData("j 12", "jal x0, 12")]
        [InlineData("jal 12", "jal x1, 12")]
        [InlineData("jr x1", "jalr x0, x1, 0")]
        [InlineData("jalr x1", "jalr x1, x1, 0")]
        [InlineData("ret", "jalr x0, x1, 0")]
        [InlineData("call 0x123456", "auipc x1, 0x123000; jalr x1, x1, 0x456")]
        [InlineData("tail -0x1000", "auipc x6, -0x1000; jalr x0, x6, 0")]
        [InlineData("negw x5, x6", "subw x5, x0, x6")]
        [InlineData("sext.w x5, x6", "addiw x5, x6, 0")]
        [InlineData("zext.b x5, x6", "andi x5, x6, 255")]
        [InlineData("csrr x5, cycle", "csrrs x5, cycle, x0")]
        [InlineData("csrwi cycle, 3", "csrrwi x0, cycle, 3")]
        [InlineData("csrsi cycle, 3", "csrrsi x0, cycle, 3")]
        [InlineData("csrci cycle, 3", "csrrci x0, cycle, 3")]
        [InlineData("rdcycle x5", "csrrs x5, cycle, x0")]
        [InlineData("rdcycleh x5", "csrrs x5, cycleh, x0")]
        [InlineData("rdtime x5", "csrrs x5, time, x0")]
        [InlineData("rdtimeh x5", "csrrs x5, timeh, x0")]
        [InlineData("rdinstret x5", "csrrs x5, instret, x0")]
        [InlineData("rdinstreth x5", "csrrs x5, instreth, x0")]
        [InlineData("frcsr x5", "csrrs x5, fcsr, x0")]
        [InlineData("frflags x5", "csrrs x5, fflags, x0")]
        [InlineData("frrm x5", "csrrs x5, frm, x0")]
        [InlineData("fscsr x5, x6", "csrrw x5, fcsr, x6")]
        [InlineData("fscsr x6", "csrrw x0, fcsr, x6")]
        [InlineData("fsflags x5, x6", "csrrw x5, fflags, x6")]
        [InlineData("fsflags x6", "csrrw x0, fflags, x6")]
        [InlineData("fsflagsi 3", "csrrwi x0, fflags, 3")]
        [InlineData("fsflagsi x5, 3", "csrrwi x5, fflags, 3")]
        [InlineData("fsrm x5, x6", "csrrw x5, frm, x6")]
        [InlineData("fsrm x6", "csrrw x0, frm, x6")]
        [InlineData("fsrmi 2", "csrrwi x0, frm, 2")]
        [InlineData("fsrmi x5, 2", "csrrwi x5, frm, 2")]
        public void PseudoInstructionIsAssembledCorrectly(string pseudo, string real)
        {
            var pseudoAssembler = new UnifiedAssembler();
            var realAssembler = new UnifiedAssembler();

            var pseudoMachineCode = pseudoAssembler.Assemble(pseudo);
            var realMachineCode = realAssembler.Assemble(real);

            pseudoMachineCode.Should().Equal(realMachineCode);
        }
    }
}
