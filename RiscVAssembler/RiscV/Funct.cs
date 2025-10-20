namespace RiscVAssembler.RiscV
{
    public static class Funct3
    {
        public const uint MUL = 0b000;
        public const uint MULH = 0b001;
        public const uint MULHSU = 0b010;
        public const uint MULHU = 0b011;
        public const uint DIV = 0b100;
        public const uint DIVU = 0b101;
        public const uint REM = 0b110;
        public const uint REMU = 0b111;
        public const uint MULW = 0b000;
        public const uint DIVW = 0b100;
        public const uint DIVUW = 0b101;
        public const uint REMW = 0b110;
        public const uint REMUW = 0b111;
    }

    public static class Funct7
    {
        public const uint MULDIV = 0b0000001;
    }
}
