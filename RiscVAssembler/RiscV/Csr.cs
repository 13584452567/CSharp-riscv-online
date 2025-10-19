// RiscVAssembler/RiscV/Csr.cs
namespace RiscVAssembler.RiscV;

public static class Csr
{
    // Minimal CSR name mapping; extend as needed
    private static readonly Dictionary<string, int> _map = new(StringComparer.OrdinalIgnoreCase)
    {
        {"ustatus", 0x000}, {"fflags", 0x001}, {"frm", 0x002}, {"fcsr", 0x003},
        {"cycle", 0xC00}, {"time", 0xC01}, {"instret", 0xC02},
        {"cycleh", 0xC80}, {"timeh", 0xC81}, {"instreth", 0xC82},
        {"sstatus", 0x100}, {"sie", 0x104}, {"stvec", 0x105}, {"scounteren", 0x106},
        {"sscratch", 0x140}, {"sepc", 0x141}, {"scause", 0x142}, {"stval", 0x143}, {"sip", 0x144},
        {"senvcfg", 0x10A},
        {"mstatus", 0x300}, {"misa", 0x301}, {"medeleg", 0x302}, {"mideleg", 0x303}, {"mie", 0x304},
        {"mtvec", 0x305}, {"mcounteren", 0x306},
        {"mscratch", 0x340}, {"mepc", 0x341}, {"mcause", 0x342}, {"mtval", 0x343}, {"mip", 0x344},
        {"menvcfg", 0x30A}, {"mcycle", 0xB00}, {"minstret", 0xB02}, {"mhartid", 0xF14},
    };

    public static bool TryGet(string name, out int addr) => _map.TryGetValue(name, out addr);
}
