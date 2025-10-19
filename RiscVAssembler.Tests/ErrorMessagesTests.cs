using System;
using FluentAssertions;
using System.Linq;
using RiscVAssembler.Assembler;
using Xunit;

namespace RiscVAssembler.Tests;

public class ErrorMessagesTests
{
    [Theory]
    [InlineData("lw x1, 4(abc)", "Unknown or invalid register 'abc'", "Use x0..x31 or ABI names")]
    [InlineData("lw x1, four(sp)", "Invalid memory operand format", "Expected 'offset(reg)'")]
    [InlineData("lw x1, (sp)", "Invalid memory operand format", "Expected 'offset(reg)'")]
    public void MemoryOperand_Should_Report_FriendlyErrors(string asm, string mustContain1, string mustContain2)
    {
        var asmblr = new UnifiedAssembler();
        Action act = () => asmblr.Assemble(asm).ToList();
        act.Should().Throw<ArgumentException>()
            .Where(ex => ex.Message.Contains(mustContain1) && ex.Message.Contains(mustContain2));
    }
}
