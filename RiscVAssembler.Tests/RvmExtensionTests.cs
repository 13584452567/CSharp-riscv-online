using FluentAssertions;
using RiscVAssembler.Assembler;
using RiscVAssembler.Decoder;
using RiscVAssembler.RiscV;
using Xunit;

namespace RiscVAssembler.Tests
{
    public class RvmExtensionTests
    {
        [Theory]
        [InlineData("mul x1, x2, x3", 0x023100B3u)]
        [InlineData("mulh x1, x2, x3", 0x023110B3u)]
        [InlineData("mulhsu x1, x2, x3", 0x023120B3u)]
        [InlineData("mulhu x1, x2, x3", 0x023130B3u)]
        [InlineData("div x1, x2, x3", 0x023140B3u)]
        [InlineData("divu x1, x2, x3", 0x023150B3u)]
        [InlineData("rem x1, x2, x3", 0x023160B3u)]
        [InlineData("remu x1, x2, x3", 0x023170B3u)]
        public void AssembleRvmInstruction(string assembly, uint expectedMachineCode)
        {
            var assembler = new UnifiedAssembler();
            var machineCode = assembler.Assemble(assembly);

            machineCode.Should().Equal(expectedMachineCode);
        }

        [Theory]
        [InlineData("mul x1, x2, x3", 0x023100B3u)]
        [InlineData("mulh x1, x2, x3", 0x023110B3u)]
        [InlineData("mulhsu x1, x2, x3", 0x023120B3u)]
        [InlineData("mulhu x1, x2, x3", 0x023130B3u)]
        [InlineData("div x1, x2, x3", 0x023140B3u)]
        [InlineData("divu x1, x2, x3", 0x023150B3u)]
        [InlineData("rem x1, x2, x3", 0x023160B3u)]
        [InlineData("remu x1, x2, x3", 0x023170B3u)]
        public void DisassembleRvmInstruction(string expectedAssembly, uint machineCode)
        {
            var disassembler = new UnifiedDisassembler();
            var assembly = disassembler.Disassemble(machineCode);

            assembly.Should().Be(expectedAssembly);
        }

        [Theory]
        [InlineData("mulw x1, x2, x3", 0x023100BB)]
        [InlineData("divw x1, x2, x3", 0x023140BB)]
        [InlineData("divuw x1, x2, x3", 0x023150BB)]
        [InlineData("remw x1, x2, x3", 0x023160BB)]
        [InlineData("remuw x1, x2, x3", 0x023170BB)]
        public void AssembleRvm64Instruction(string assembly, uint expectedMachineCode)
        {
            var assembler = new UnifiedAssembler();
            var machineCode = assembler.Assemble(assembly);

            machineCode.Should().Equal(expectedMachineCode);
        }

        [Theory]
        [InlineData("mulw x1, x2, x3", 0x023100BB)]
        [InlineData("divw x1, x2, x3", 0x023140BB)]
        [InlineData("divuw x1, x2, x3", 0x023150BB)]
        [InlineData("remw x1, x2, x3", 0x023160BB)]
        [InlineData("remuw x1, x2, x3", 0x023170BB)]
        public void DisassembleRvm64Instruction(string expectedAssembly, uint machineCode)
        {
            var disassembler = new UnifiedDisassembler(Xlen.X64);
            var assembly = disassembler.Disassemble(machineCode);

            assembly.Should().Be(expectedAssembly);
        }
    }
}
