using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pixel2Svg
{
    public struct Vector2
    {
        public int x;
        public int y;
        public Vector2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
        public override bool Equals(object obj)
        {
            if (obj is Vector2)
            {
                var p = (Vector2)obj;
                return p.x == x && p.y == y;
            }
            return false;
        }
        public override int GetHashCode()
        {
            unchecked//使用了unchecked则不会检查溢出
            { // Overflow is fine, just wrap
                int hash = (int)2166136261;
                // Suitable nullity checks etc, of course :)
                hash = (hash * 16777619) ^ x.GetHashCode();
                hash = (hash * 16777619) ^ y.GetHashCode();
                return hash;
            }
        }
    }
}
