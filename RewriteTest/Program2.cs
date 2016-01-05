using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RewriteTest
{
    class Program2
    {
        /*static async Task<int> Test3()
        {
            await Task.Delay(100);
            return 3;
        }*/

        public static async Task<int> Test ()
        {
            int abcdEFGH = 0;
            int c = 3;
            await Task.Delay (100 + abcdEFGH + c);
            await Task.Delay(100 + abcdEFGH + c);
            await Task.Delay(100 + abcdEFGH + c);
            return abcdEFGH + c;
        }
    }
}
