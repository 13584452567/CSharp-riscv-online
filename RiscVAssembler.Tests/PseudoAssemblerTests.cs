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
