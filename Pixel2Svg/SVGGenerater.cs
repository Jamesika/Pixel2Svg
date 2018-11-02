using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Pixel2Svg
{
    public class SVGGenerator
    {
        string filePath;
        List<string> polygons;

        int width;
        int height;
        Color[,] colorData;

        public void Open(string path)
        {
            filePath = path;
            // 读取图片
            BitmapImage sourceImage = new BitmapImage();
            sourceImage.BeginInit();
            sourceImage.UriSource = new Uri(AppDomain.CurrentDomain.BaseDirectory + path, UriKind.Absolute);//打开图片
            sourceImage.EndInit();
            BitmapSource pixelImage = sourceImage;

            // 设置图片格式 rgba
            FormatConvertedBitmap newFormatedBitmapSource = new FormatConvertedBitmap();
            newFormatedBitmapSource.BeginInit();
            newFormatedBitmapSource.Source = pixelImage;
            newFormatedBitmapSource.DestinationFormat = PixelFormats.Bgra32;
            newFormatedBitmapSource.EndInit();
            pixelImage = newFormatedBitmapSource;

            // 获得图片数据
            width = pixelImage.PixelWidth;
            height = pixelImage.PixelHeight;

            int stride = pixelImage.PixelWidth * 4;
            byte[] pixels = new byte[pixelImage.PixelHeight * stride];
            pixelImage.CopyPixels(pixels, stride, 0);
            colorData = new Color[width, height];

            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                {
                    colorData[i, j].b = pixels[(j * width + i) * 4];
                    colorData[i, j].g = pixels[(j * width + i) * 4 + 1];
                    colorData[i, j].r = pixels[(j * width + i) * 4 + 2];
                    colorData[i, j].a = pixels[(j * width + i) * 4 + 3];
                }
        }
        // 找到多边形信息, 绘制出每一个多边形
        public void ToSvg()
        {
            HashSet<Vector2> points = new HashSet<Vector2>();
            List<HashSet<Vector2>> areas = new List<HashSet<Vector2>>();

            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                {
                    if (points.Contains(new Vector2(i, j)))
                        continue;
                    // 如果透明则不处理
                    if (colorData[i, j].a == 0)
                        continue;
                    var ps = FloodFill(i, j);
                    areas.Add(ps);
                    foreach (var p in ps)
                        points.Add(p);
                }

            // 转化为字符串
            polygons = new List<string>();
            foreach (var a in areas)
            {
                var s = PixelsToPolygon(a);
                polygons.Add(s);
            }
        }

        public void Save(string path)
        {
            filePath = Path.ChangeExtension(filePath, ".svg");
            StreamWriter sw = new StreamWriter(filePath, false, Encoding.Default);
            sw.WriteLine("<?xml version =\"1.0\" standalone = \"no\"?>");
            sw.WriteLine("<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\"");
            sw.WriteLine("\n\r\"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\">");
            sw.WriteLine("<svg width = \"100%\" height = \"100%\" version = \"1.1\"");
            sw.WriteLine("xmlns = \"http://www.w3.org/2000/svg\" >");

            foreach (var s in polygons)
                sw.WriteLine(s);

            sw.WriteLine("</svg>");
            sw.Flush();
            sw.Close();
        }
        // 找到所有临近的点
        HashSet<Vector2> FloodFill(int x, int y)
        {
            Color c = colorData[x, y];
            HashSet<Vector2> ps = new HashSet<Vector2>();
            Queue<Vector2> edges = new Queue<Vector2>();
            ps.Add(new Vector2(x, y));
            edges.Enqueue(new Vector2(x, y));

            int[] dx = { 0, 0, -1, 1 };
            int[] dy = { 1, -1, 0, 0 };

            while (edges.Count != 0)
            {
                var p = edges.Dequeue();
                for (int i = 0; i < 4; i++)
                {
                    Vector2 dp = new Vector2(p.x + dx[i], p.y + dy[i]);
                    if (!IsValidPos(dp) || ps.Contains(dp))
                        continue;
                    if (colorData[dp.x, dp.y].Equals(c))
                    {
                        ps.Add(dp);
                        edges.Enqueue(dp);
                    }
                }
            }
            return ps;
        }

        bool IsValidPos(Vector2 p)
        {
            return (0 <= p.x && p.x < width && p.y >= 0 && p.y < height);
        }
        // 将一系列像素转化为多边形
        // 三角形示例
        // <polygon points="290,100 300,210 170,530" style="fill:rgba(200,100,50,0.5)"/>
        string PixelsToPolygon(HashSet<Vector2> points)
        {
            // 1. 找到一个角点(左上角)
            Vector2 startPos = new Vector2(0, 0);
            foreach (var pos in points)
            {
                if (IsPolygonVert(pos.x * 2 - 1, pos.y * 2 + 1, points))
                {
                    startPos = new Vector2(pos.x * 2 - 1, pos.y * 2 + 1);
                    break;
                }
            }

            // 2. 开始构建多边形
            Queue<Vector2> polygon = new Queue<Vector2>();
            polygon.Enqueue(startPos);

            Vector2 curPos = startPos;
            Vector2 direc = new Vector2(0, 0);

            while (true)
            {
                // 找到一个方向
                int[] dx = { -1, 1, 0, 0 };
                int[] dy = { 0, 0, -1, 1 };
                for (int i = 0; i < 4; i++)
                {
                    Vector2 newDirec = new Vector2(dx[i] * 2, dy[i] * 2);
                    if (newDirec.Equals(new Vector2(-direc.x, -direc.y)))
                        continue;
                    // 当前方向下, 下一个位置应该是边缘, 且不应该(跨越区域 || 跨越空格)
                    // 检测是否跨越区域, 找到半途上, 两边的点, 如果两个点都(是 || 不是)像素点, 说明正在穿越区域
                    Vector2 nextPos = new Vector2(curPos.x + newDirec.x / 2, curPos.y + newDirec.y / 2);
                    Vector2 pos1;
                    Vector2 pos2;

                    if (newDirec.x != 0)
                    {
                        pos1 = new Vector2(nextPos.x / 2, (nextPos.y + 1) / 2);
                        pos2 = new Vector2(nextPos.x / 2, (nextPos.y - 1) / 2);
                    }
                    else
                    {
                        pos1 = new Vector2((nextPos.x + 1) / 2, nextPos.y / 2);
                        pos2 = new Vector2((nextPos.x - 1) / 2, nextPos.y / 2);
                    }
                    // 说明正在穿越区域
                    if (points.Contains(pos1) == points.Contains(pos2))
                        continue;
                    if (IsPolygonEdge(curPos.x + newDirec.x, curPos.y + newDirec.y, points))
                    {
                        direc = newDirec;
                        break;
                    }
                }

                // 沿方向前进
                while (IsPolygonEdge(curPos.x + direc.x, curPos.y + direc.y, points))
                {
                    curPos = new Vector2(curPos.x + direc.x, curPos.y + direc.y);
                    // 如果沿途遇见了顶点
                    if (IsPolygonVert(curPos.x, curPos.y, points))
                    {
                        break;
                    }
                }

                if (!IsPolygonVert(curPos.x, curPos.y, points))
                {
                    direc = new Vector2(1, 0);
                }

                if (curPos.Equals(startPos))
                    break;
                else
                    polygon.Enqueue(curPos);
            }

            // 3. 生成字符串
            string poly = "";
            poly += "<polygon points = \"";
            // 顶点
            foreach (var p in polygon)
            {
                poly += p.x.ToString();
                poly += ",";
                poly += p.y.ToString();
                poly += " ";
            }
            poly += "\" style=\"fill:rgba(";
            // 颜色
            var colorPoint = points.First();
            var color = colorData[colorPoint.x, colorPoint.y];
            poly += color.r.ToString() + ",";
            poly += color.g.ToString() + ",";
            poly += color.b.ToString() + ",";
            poly += (color.a / 255f).ToString();
            poly += ")\"/>";
            return poly;
        }
        // 只要四个角之一有像素, 且不全有像素, 就是多边形的边
        bool IsPolygonEdge(int x, int y, HashSet<Vector2> points)
        {
            int[] dx = { -1, -1, 1, 1 };
            int[] dy = { -1, 1, -1, 1 };

            int count = 0;
            for (int i = 0; i < 4; i++)
            {
                Vector2 p = new Vector2((x + dx[i]) / 2, (y + dy[i]) / 2);
                if (points.Contains(p))
                    count++;
            }
            return count > 0 && count < 4;
        }
        // 只要四个角有奇数个像素, 就是多边形的角(因为使用的是四连通)
        bool IsPolygonVert(int x, int y, HashSet<Vector2> points)
        {
            int[] dx = { -1, -1, 1, 1 };
            int[] dy = { -1, 1, -1, 1 };

            int count = 0;
            for (int i = 0; i < 4; i++)
            {
                Vector2 p = new Vector2((x + dx[i]) / 2, (y + dy[i]) / 2);
                if (points.Contains(p))
                    count++;
            }
            return (count % 2 != 0);
        }
    }
}
