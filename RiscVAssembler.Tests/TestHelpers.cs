using System.Linq;
using RiscVAssembler.Assembler;

namespace RiscVAssembler.Tests;

public static class TestHelpers
{
    public static uint[] Assemble(string code)
    {
        var ua = new UnifiedAssembler();
        return ua.Assemble(code).ToArray();
    }
}
