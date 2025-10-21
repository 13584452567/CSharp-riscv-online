namespace RiscVAssembler.Assembler;

public static class AssemblySymbols
{
    // Symbol table: label -> absolute byte address
    public static Dictionary<string,int>? Symbols;

    // Current instruction address (byte offset) used during assembly
    public static int CurrentPc { get; set; }

    // When true, resolving a label yields a relative offset (labelAddr - CurrentPc)
    public static bool TreatLabelAsRelative { get; set; }

    public static bool TryResolve(string token, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(token)) return false;
        token = token.Trim();

        // Numeric forms handled by callers first if needed, but provide simple hex/dec support here
        if (token.StartsWith("-0x") || token.StartsWith("+0x") || token.StartsWith("0x") )
        {
            try
            {
                if (token.StartsWith("-0x")) { value = -Convert.ToInt32(token[3..], 16); return true; }
                if (token.StartsWith("+0x")) { value = Convert.ToInt32(token[3..], 16); return true; }
                value = Convert.ToInt32(token, 16); return true;
            }
            catch { }
        }
        if (int.TryParse(token, out var v)) { value = v; return true; }

        if (Symbols != null && Symbols.TryGetValue(token, out var addr))
        {
            if (TreatLabelAsRelative)
                value = addr - CurrentPc;
            else
                value = addr;
            return true;
        }

        return false;
    }
}
