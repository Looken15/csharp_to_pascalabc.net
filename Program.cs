using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ex
{
    class Program
    {
        static int Inc(int a, string b, byte c)
        {
            a += b.Length;
            return a + c;
        }

        static void Pr()
        {
            Console.WriteLine("privet");
        }

        static void Main(string[] args)
        {
            int b = 2; //Это комментарий
            double a;
            a = b;
            char x, y, z;
            string s = "PascalABC forever";
            int[] arr = { 1, 2, 3, 4, 5 };
            bool m1 = arr[2] == arr[4];
            Console.WriteLine(m1);
            while (b != 5)
            {
                b += 1;
                a *= b;
            }
            /*
            Console.WriteLine(a);
            */
            int c = 0;
            for (var i = 0; i < 5; i += 1)
            {
                c = i;
                b += c;
            }
            if (a == c)
            {
                Console.WriteLine(15);
            }
            else
            {
                Console.WriteLine(55);
            }
            var res = Inc(5, "b", 001);
            Pr();
            double[] vs = new double[arr.Length];
            int j = 0;
            foreach (var t in arr)
            {
                vs[j] = t;
                j = j + 1;
                //j++;
                //++j;
                //j--;
            }
        }
    }
}
