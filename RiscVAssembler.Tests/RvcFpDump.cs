using System;
using Xunit;

namespace RiscVAssembler.Tests
{
    public class RvcFpDump
    {
        [Fact]
        public void DumpEncodings()
        {
            void D(string asm)
            {
                var words = TestHelpers.Assemble(asm);
                foreach (var w in words) Console.WriteLine($"{asm} -> 0x{w:X8}");
            }

            D("c.flw f8, 0(a0)");
            D("c.fsw f8, 0(a0)");
            D("c.flwsp f2, 0(sp)");
            D("c.fswsp f1, 8(sp)");
            D("c.fld f8, 0(a0)");
            D("c.fsd f8, 0(a0)");
            D("c.fldsp f2, 16(sp)");
            D("c.fsdsp f3, 24(sp)");
        }
    }
}
