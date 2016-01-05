using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace RewriteApp
{
    class Program
    {
        static void Main(string[] args)
        {
            //File.Copy ("RewriteTest.exe", "RewriteTest2.exe", true);
            //File.Copy ("RewriteTest.pdb", "RewriteTest2.pdb", true);
            var assembly = AssemblyDefinition.ReadAssembly(@"RewriteTest.exe", new ReaderParameters { ReadSymbols = true, ReadingMode = ReadingMode.Immediate });
            assembly.Write("RewriteTest2.exe", new WriterParameters { WriteSymbols = true });
        }
    }
}
