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
