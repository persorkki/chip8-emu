using System.Collections.Generic;
using System.IO;

using SFML.System;
using SFML.Graphics;
using SFML.Window;

using Extensions;
using EmulatorExtensions;

namespace emud
{
    class Emulator
    {
        //registers 0-f
        //f is used for flags
        
        byte[] registers = new byte[16];

        //used only for storing addresses (12bits)
        ushort registerI;

        byte delaytimer;
        byte soundtimer;

        //program counter 
        //0x200 is where the program starts
        ushort pc = 0x200;

        //stack pointer
        byte sp;

        //4KB RAM memory, first 512 bytes are not used for programs
        readonly byte[] memory = new byte[4096];

        //64,32-pixel monochrome display
        byte[,] display = new byte[64,32];

        //16 16-bit register stack
        Stack<ushort> stack = new Stack<ushort>(16);

        RenderWindow window;
        //main constructor, use test program if no other ROM is given
        public Emulator(string ROM = "IBM Logo.ch8")
        {
            Initialize();
            LoadROM(ROM);
            Run();
        }
        public void Initialize()
        {
            memory.ResetArray(0x00);
            registers.ResetArray(0x00);
            display.ResetArray(0x00);
            window = new RenderWindow(new VideoMode(640,320), "Chip-8 emu");
            window.SetFramerateLimit(60);
        }

        public void LoadROM(string ROM)
        {
            //chip-8 programs start at 0x200, so we first load the game into a array
            //then copy it to the memory byte array with the offset 0x200
            byte[] file = File.ReadAllBytes(ROM);
            for (int i=0; i<file.Length;i++)
            {
                memory[0x200+i] = file[i];
            }
        }
        public void DisplayScreen()
        {
            window.Clear();
            Vector2f point = new(10,10);
            RectangleShape rect = new(point);

            for (int y=0; y<display.GetLongLength(1); y++)
            {
                for (int x=0; x<display.GetLongLength(0); x++)
                {
                    if (display[x,y] != 0)
                    {
                        rect.FillColor = new Color(255,255,255);
                    }
                    else
                    {
                        rect.FillColor = new Color(25,25,25);
                    }
                    rect.Position = new(x*10, y*10);
                    window.Draw(rect);
                }
            }
            window.Display();
        }
        public void Step()
        {
            ushort opcode = (ushort) ((memory[pc] << 8) | memory[pc+1]) ;

            /*
            nnn or addr - A 12-bit value, the lowest 12 bits of the instruction
            n or nibble - A 4-bit value, the lowest 4 bits of the instruction
            x - A 4-bit value, the lower 4 bits of the high byte of the instruction
            y - A 4-bit value, the upper 4 bits of the low byte of the instruction
            kk or byte - An 8-bit value, the lowest 8 bits of the instruction
            */
            
            byte x = (byte) ((opcode & 0x0f00) >> 8);
            byte y = (byte) ((opcode & 0x00f0) >> 4);
            ushort addr = (ushort) (opcode & 0x0fff);
            byte lastN = (byte) (opcode & 0x000f);
            byte firstN = (byte) ((opcode & 0xf000) >> 12);
            byte firstbyte = memory[pc];
            byte kk = memory[pc+1];
            opcode.PrintRaw();
            switch(firstN)
            {
                case 0x0:
                    switch(kk)
                    {

                        //00E0 - CLS
                        //clears the screen
                        case 0xe0:
                            System.Console.Write("CLS");
                            display.ResetArray(0x00);
                            break;

                        //00EE - RET
                        //The interpreter sets the program counter to the address at the top of the stack,
                        //then subtracts 1 from the stack pointer.
                        ///<summary>
                        case 0xee:
                            pc = stack.Pop();
                            sp--;
                            System.Console.Write("RET");
                            break;
                        //empty
                        default:
                            System.Console.WriteLine("?");
                            break;
                    }
                    break;

                //1nnn - JP addr
                //sets program counter to addr
                case 0x1:
                    pc = addr;
                    System.Console.Write($"JP {addr:x}");
                    break;
                
                //2nnn - CALL addr
                //The interpreter increments the stack pointer, then puts the current PC on the top of the stack. 
                //The PC is then set to nnn.
                case 0x2:
                    sp++;
                    stack.Push(pc);
                    pc = addr;
                    System.Console.Write($"CALL {addr:x}");
                    break;

                //3xkk - SE Vx, byte
                //The interpreter compares register Vx to kk, and if they are equal, increments the program counter by 2.
                case 0x3:
                    if (registers[x] == kk)
                    {
                        pc += 2;
                    }
                    System.Console.Write($"SE V{x:x}, {kk:x} ");
                    break;

                //4xkk - SNE Vx, byte
                //The interpreter compares register Vx to kk, and if they are not equal, increments the program counter by 2.
                case 0x4:
                    if (registers[x] != kk)
                    {
                        pc += 2;
                    }
                    System.Console.Write($"SNE V{x:x}, {kk:x} ");
                    break;

                //5xy0 - SE Vx, Vy
                //The interpreter compares register Vx to register Vy, and if they are equal, 
                //increments the program counter by 2.
                case 0x5:
                    if (registers[x] == registers[y])
                    {
                        pc += 2;
                    }
                    System.Console.Write($"SE V{x:x}, V{y:x} ");
                    break;

                //6xkk - LD Vx, byte
                //The interpreter puts the value kk into register Vx.
                case 0x6:
                    registers[x] = kk;
                    System.Console.Write($"LD V{x:x}, {kk:x} ");
                    break;

                //7xkk - ADD Vx, byte
                //Adds the value kk to the value of register Vx, then stores the result in Vx.
                case 0x7:
                    registers[x] += kk;
                    System.Console.Write($"ADD V{x:x}, {kk}");
                    break;

                //Annn - LD I, addr
                //The value of register I is set to nnn.
                case 0xa:
                    registerI = addr;
                    System.Console.Write($"LD I, {addr:x}");
                    break;
                //Dxyn - DRW Vx, Vy, nibble
                //Display n-byte sprite starting at memory location I at (Vx, Vy), set VF = collision.
                
                case 0xd:
                    int posX;
                    int posY;
                    registers[15] = 0;
                    for(int height = 0; height < lastN; height++)
                    {
                        for (int width = 0; width < 8; width++)
                        {
                            posX = registers[x] + width;
                            posY = registers[y] + height;

                            if ((memory[registerI+height] & (0x80 >> width)) != 0)
                            {
                                if (display[posX,posY] == 1)
                                {
                                    registers[15] = 1;
                                }
                                display[posX, posY] ^= 1;
                            }
                            
                        }
                    }
                    System.Console.Write($"DRW V{x:x}, V{y:x}, {lastN:x}");
                    break;
                default:
                    System.Console.Write("?");
                    break;
            }
            System.Console.WriteLine("");
        }
        

        public void Run()
        {
            while(pc < memory.Length)
            {
                DisplayScreen();
                Step();
                pc += 2;
            }
        }
    }
}
