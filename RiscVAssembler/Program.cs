// RiscVAssembler/Program.cs
using RiscVAssembler.Assembler;
using RiscVAssembler.Decoder;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("RISC-V Assembler/Disassembler");
        Console.WriteLine("-----------------------------");

        if (args.Length == 0)
        {
            ShowHelp();
            return;
        }

        var command = args[0].ToLower();
        var arguments = args.Skip(1).ToArray();

        switch (command)
        {
            case "asm":
                if (arguments.Length > 0)
                {
                    AssembleCode(string.Join(" ", arguments));
                }
                else
                {
                    Console.WriteLine("Error: Missing code to assemble. Use 'asm \"<your code>\"'.");
                }
                break;
            
            case "disasm":
                if (arguments.Length > 0)
                {
                    DisassembleCode(arguments);
                }
                else
                {
                    Console.WriteLine("Error: Missing machine code to disassemble. Use 'disasm [--xlen=32|64|128] <hex_code_1> ...'.");
                }
                break;

            default:
                Console.WriteLine($"Error: Unknown command '{command}'");
                ShowHelp();
                break;
        }
    }

    static void AssembleCode(string code)
    {
        try
        {
            var assembler = new UnifiedAssembler();
            var machineCode = assembler.Assemble(code).ToList();
            
            Console.WriteLine("\nAssembly Code:");
            Console.WriteLine(code);
            
            Console.WriteLine("\nMachine Code (Hex):");
            foreach (var instruction in machineCode)
            {
                Console.WriteLine($"0x{instruction:X8}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError during assembly: {ex.Message}");
        }
    }

    static void DisassembleCode(string[] args)
    {
        try
        {
            // Parse optional --xlen flag
            Xlen xlen = Xlen.Unknown;
            var hexCodes = new List<string>();
            foreach (var a in args)
            {
                if (a.StartsWith("--xlen=", StringComparison.OrdinalIgnoreCase))
                {
                    var val = a.Split('=', 2)[1];
                    xlen = val switch
                    {
                        "32" => Xlen.X32,
                        "64" => Xlen.X64,
                        "128" => Xlen.X128,
                        _ => throw new ArgumentException("--xlen must be 32, 64, or 128")
                    };
                }
                else
                {
                    hexCodes.Add(a);
                }
            }

            if (hexCodes.Count == 0) throw new ArgumentException("Missing hex codes to disassemble");

            var disassembler = new UnifiedDisassembler(xlen);
            var instructions = new List<uint>();

            foreach (var hex in hexCodes)
            {
                instructions.Add(Convert.ToUInt32(hex, 16));
            }

            Console.WriteLine("\nMachine Code (Hex) -> Assembly:");
            foreach (var instruction in instructions)
            {
                var assembly = disassembler.Disassemble(instruction);
                Console.WriteLine($"0x{instruction:X8} -> {assembly}");
            }
        }
        catch (FormatException)
        {
            Console.WriteLine("\nError: Invalid hex format. Ensure all codes are valid 32-bit hex numbers (e.g., 0x1234ABCD).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError during disassembly: {ex.Message}");
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine("\nUsage:");
        Console.WriteLine("  RiscVAssembler <command> [options]");
        Console.WriteLine("\nCommands:");
        Console.WriteLine("  asm \"<code>\"          Assemble the given RISC-V code.");
        Console.WriteLine("  disasm [--xlen=32|64|128] <hex>...      Disassemble one or more 32-bit hex machine codes.");
        Console.WriteLine("\nExamples:");
        Console.WriteLine("  RiscVAssembler asm \"addi x1, zero, 42; lw x2, 4(sp)\"");
        Console.WriteLine("  RiscVAssembler disasm --xlen=64 0x02A00093 0x00412103");
    }
}
