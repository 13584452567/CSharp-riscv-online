using FluentAssertions;
using Xunit;

namespace RiscVAssembler.Tests;

public class RvcFpTests
{
    private static uint[] A(string asm) => TestHelpers.Assemble(asm);

    [Fact]
    public void CFlw_Assembles()
    {
        // c.flw f8, 0(a0)  -> uses rd' f8 -> f8 corresponds to prime fregs f8..f15 => rd' index 0
        var words = A("c.flw f8, 0(a0)");
        words.Should().HaveCount(1);
        words[0].Should().Be(0x00004100u);
    }

    [Fact]
    public void CFsw_Assembles()
    {
        var words = A("c.fsw f8, 0(a0)");
        words.Should().HaveCount(1);
        words[0].Should().Be(0x0000C100u);
    }

    [Fact]
    public void CFlwSp_Assembles()
    {
        var words = A("c.flwsp f2, 0(sp)");
        words.Should().HaveCount(1);
        words[0].Should().Be(0x00004102u);
    }

    [Fact]
    public void CFswSp_Assembles()
    {
        var words = A("c.fswsp f1, 8(sp)");
        words.Should().HaveCount(1);
        words[0].Should().Be(0x0000C406u);
    }

    [Fact]
    public void CFld_Assembles()
    {
        var words = A("c.fld f8, 0(a0)");
        words.Should().HaveCount(1);
        words[0].Should().Be(0x00006100u);
    }

    [Fact]
    public void CFsd_Assembles()
    {
        var words = A("c.fsd f8, 0(a0)");
        words.Should().HaveCount(1);
        words[0].Should().Be(0x0000E100u);
    }

    [Fact]
    public void CFldSp_Assembles()
    {
        var words = A("c.fldsp f2, 16(sp)");
        words.Should().HaveCount(1);
        words[0].Should().Be(0x00006142u);
    }

    [Fact]
    public void CFsdSp_Assembles()
    {
        var words = A("c.fsdsp f3, 24(sp)");
        words.Should().HaveCount(1);
        words[0].Should().Be(0x0000EC0Eu);
    }
}
