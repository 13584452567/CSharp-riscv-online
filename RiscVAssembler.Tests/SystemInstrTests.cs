using Xunit;

namespace RiscVAssembler.Tests;

public class SystemInstrTests
{
    [Fact]
    public void PrivilegedAndSystemEncodings()
    {
        // Expected encodings per RISC-V privileged spec
        Assert.Equal(new uint[] { 0x30200073u }, TestHelpers.Assemble("mret"));
        Assert.Equal(new uint[] { 0x10200073u }, TestHelpers.Assemble("sret"));
        Assert.Equal(new uint[] { 0x00200073u }, TestHelpers.Assemble("uret"));
        Assert.Equal(new uint[] { 0x10500073u }, TestHelpers.Assemble("wfi"));
        Assert.Equal(new uint[] { 0x12000073u }, TestHelpers.Assemble("sfence.vma"));
        Assert.Equal(new uint[] { 0x12200073u }, TestHelpers.Assemble("sfence.vm"));
    }
}
