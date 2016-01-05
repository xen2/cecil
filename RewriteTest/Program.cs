using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RewriteTest
{
    class Program
    {
        /*static async Task<int> Test3()
        {
            await Task.Delay(100);
            return 3;
        }*/

        static async Task<int> Test ()
        {
            int abcdEFGH = 0;
            int c = 3;
            await Task.Delay (100 + abcdEFGH + c);
            return abcdEFGH + c;
        }

        /*static IEnumerable<int> Test2 ()
        {
            int a = 0;
            yield return a;
            a++;
            yield return a;
        } */

        static void Main(string[] args)
        {
            Test ().Wait ();
            Program2.Test ().Wait ();
            //foreach (var i in Test2 ()) {}
            Console.WriteLine("Hello");
        }
    }
}
