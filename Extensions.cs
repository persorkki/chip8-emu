namespace Extensions
{
    public static class OpCodeExtensions
    {
        ///<summary>prints a byte array</summary>
        public static void PrintRawArray(this byte[] arr)
        {
            for (int i=0;i<arr.Length;i++)
            {
                PrintRaw(arr[i]);
            }
        }
        public static void PrintRaw(this ushort opcode)
        {
            System.Console.Write($"[{opcode:x4}] ");
        }
        public static void Disassemble(this ushort opcode)
        {
            switch(opcode & 0xf000 >> 12)
            {
                case 0x0:
                    System.Console.WriteLine("0x0");
                    break;

            }
        }
    }
}

namespace EmulatorExtensions
{
    public static class EmulatorUtilityExtensions
    {
        public static byte[] ResetArray(this byte[] arr, byte b)
        {
            for (int i=0; i<arr.Length; i++)
            {
                arr[i] = b;
            }
            return arr;
        }
        public static byte[,] ResetArray(this byte[,] arr, byte b)
        {
            for (int y=0; y<arr.GetLongLength(1); y++)
            {
                for (int x=0; x<arr.GetLongLength(0); x++)
                {
                    arr[x,y] = b;
                }
            }
            return arr;
        }
    }
}