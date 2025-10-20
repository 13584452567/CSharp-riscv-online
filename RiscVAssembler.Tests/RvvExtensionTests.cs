using FluentAssertions;
using RiscVAssembler.Assembler;
using Xunit;

namespace RiscVAssembler.Tests
{
    public class RvvExtensionTests
    {
        [Fact]
        public void AssembleVle32()
        {
            var assembler = new UnifiedAssembler();
            var machineCode = assembler.Assemble("vle32.v v10, (x10), v0.t");

            // Placeholder test
            machineCode.Should().Equal(0x00257557);
        }
    }
}
