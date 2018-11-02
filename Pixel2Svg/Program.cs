using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 

namespace Pixel2Svg
{
    class Program
    {
        static void Main(string[] args)
        {
            SVGGenerator fac = new SVGGenerator();

            string path = "";
            do
            {
                Console.Clear();
                Console.Write("输入图片相对路径 : ");
                path = Console.ReadLine();
            }
            while (path.Equals(""));
            
            fac.Open(path);
            fac.ToSvg();
            fac.Save(path);
        }
    }
}

