using System;
using System.Linq;
using Mono.Cecil;

class SeeFwd
{
    static void Main()
    {
        var asm = AssemblyDefinition.ReadAssembly(@"C:\Users\Yuri\Documents\RotaDoBau\refs\UnityEngine.dll");
        Console.WriteLine("ASM " + asm.Name.Name);
        foreach (var e in asm.MainModule.ExportedTypes)
        {
            string n = e.Name;
            if (n == "MonoBehaviour" || n == "Color" || n == "Rect" || n == "Screen" || n == "Vector2" || n == "Texture" || n == "Object")
                Console.WriteLine(e.FullName + " SCOPE " + e.Scope);
        }
    }
}
