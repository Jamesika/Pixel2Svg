using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pixel2Svg
{
    public struct Color
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        public Color(byte r, byte g, byte b, byte a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public override bool Equals(object obj)
        {
            if (obj is Color)
            {
                Color o = (Color)obj;
                return r == o.r && g == o.g && b == o.b && a == o.a;
            }
            return false;
        }
        public override int GetHashCode()
        {
            unchecked//使用了unchecked则不会检查溢出
            { // Overflow is fine, just wrap
                int hash = (int)2166136261;
                // Suitable nullity checks etc, of course :)
                hash = (hash * 16777619) ^ r.GetHashCode();
                hash = (hash * 16777619) ^ g.GetHashCode();
                hash = (hash * 16777619) ^ b.GetHashCode();
                hash = (hash * 16777619) ^ a.GetHashCode();
                return hash;
            }
        }
    }
}
