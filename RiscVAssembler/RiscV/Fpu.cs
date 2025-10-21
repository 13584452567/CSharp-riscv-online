// RiscVAssembler/RiscV/Fpu.cs
namespace RiscVAssembler.RiscV;

public static class Fpu
{
    // OP_FP funct7 encodings for single-precision
    public const uint FADD_S  = 0b0000000;
    public const uint FSUB_S  = 0b0000100;
    public const uint FMUL_S  = 0b0001000;
    public const uint FDIV_S  = 0b0001100;
    public const uint FSQRT_S = 0b0101100; // rs2 = 0
    public const uint FSGNJ_S = 0b0010000; // funct3 selects fsgnj/fsgnjn/fsgnjx
    public const uint FMINMAX_S = 0b0010100; // funct3 selects min/max
    public const uint FCVT_W_S = 0b1100000; // rs2 = 0b00000 (W) or 0b00001 (WU)
    public const uint FCVT_S_W = 0b1101000; // rs2 = 0b00000 (W) or 0b00001 (WU)
    public const uint FMV_X_W  = 0b1110000; // funct3=000, rs2=00000
    public const uint FMV_W_X  = 0b1111000; // funct3=000, rs2=00000
    public const uint FCLASS_S = 0b1110000; // fclass.s (funct7)
    public const uint FCMP_S   = 0b1010000; // feq/flt/fle (rm selects op)

    // OP_FP funct7 encodings for double-precision (D) - mirror S with distinct codes
    public const uint FADD_D  = 0b0000001;
    public const uint FSUB_D  = 0b0000101;
    public const uint FMUL_D  = 0b0001001;
    public const uint FDIV_D  = 0b0001101;
    public const uint FSQRT_D = 0b0101101;
    public const uint FSGNJ_D = 0b0010001;
    public const uint FMINMAX_D = 0b0010101;
    public const uint FCVT_W_D = 0b1100001;
    public const uint FCVT_D_W = 0b1101001;
    public const uint FMV_X_D  = 0b1110001;
    public const uint FMV_D_X  = 0b1111001;
    public const uint FCLASS_D = 0b1110001;
    public const uint FCMP_D   = 0b1010001;

    // FP conversions between single and double (assumed funct7 values)
    // These follow the project's pattern of 'D' variants being odd vs 'S' even.
    // From riscv-opcodes (rv_d): fcvt.s.d has 31..27=0x08 with 26..25=0 for S/D variants;
    // fcvt.s.d (fcvt.s.d) uses 31..27=0x08 and bit25..31 mapping yields funct7 = 0b0001000 (0x08)
    // fcvt.d.s uses 31..27=0x08 but with 26..25=1 -> funct7 has low bit set (0x09)
    public const uint FCVT_S_D = 0b0001000; // 0x08
    public const uint FCVT_D_S = 0b0001001; // 0x09

    // Comparison selectors for floating-point comparisons (use in funct3/rm fields as project pattern)
    // We'll use the existing FSGNJ/FMINMAX pattern and reserve selectors for comparisons
    // (these values are used as funct3 selector in BuildFpRTypeGeneric variants)
    public const uint FCMP_EQ = 0b000u; // feq
    public const uint FCMP_LT = 0b001u; // flt
    public const uint FCMP_LE = 0b010u; // fle

    // 64-bit integer conversion funct7 placeholders (assumed values following pattern)
    // From riscv-opcodes (rv64_d / rv64_f): fcvt.l.d uses 31..27=0x18 for the fcvt.l family
    // 31..27=0x18 -> funct7 bits [31..25] = 0b11000x?; mapping lower bits from 26..25
    // For fcvt.l.d (24..20=2) the documented 31..27=0x18 corresponds to funct7 = 0b1100000 (0x60)
    // For fcvt.d.l the 31..27=0x1A corresponds to funct7 = 0b1101001 (0x69) for D variants
    public const uint FCVT_L_D  = 0b1100000; // 0x60
    public const uint FCVT_D_L  = 0b1101001; // 0x69

    // Fused multiply-add opcodes (R4-type)
    public const uint MADD_S  = 0b1000011;
    public const uint MSUB_S  = 0b1000111;
    public const uint NMSUB_S = 0b1001011;
    public const uint NMADD_S = 0b1001111;

    // Rounding mode encodings
    public static uint RmFromString(string? rm) => (rm ?? "rne").ToLower() switch
    {
        "rne" => 0b000u,
        "rtz" => 0b001u,
        "rdn" => 0b010u,
        "rup" => 0b011u,
        "rmm" => 0b100u,
        "dyn" => 0b111u,
        _ => 0b000u
    };
}
