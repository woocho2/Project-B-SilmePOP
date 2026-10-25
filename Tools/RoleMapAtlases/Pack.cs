using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

// Repack existing RGBA artwork only; no illustration content is generated or repainted.
public static class RoleAtlasPack
{
    public sealed class Atlas { public string Role,Path,Guid;public int CellWidth,CellHeight,Columns,Rows; }
    public sealed class Migration { public string OldGuid,OldId,NewGuid,NewId,Name; }
    public sealed class Result { public Atlas[] Atlases; public Migration[] References; }
    static readonly string[] Themes={"Basic","Spring","Summer","Autumn","Winter","Sunny","Night","Cloudy"};
    static string Id(string s) {using(var h=MD5.Create())return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes("SlimePop.RoleAtlas.v1."+s))).Replace("-","").ToLowerInvariant();}
    static float N(string s){return Single.Parse(s,CultureInfo.InvariantCulture);}
    static string F(float n){return n.ToString("0.###",CultureInfo.InvariantCulture);}
    static string GuidOf(string meta){return Regex.Match(meta,@"(?m)^guid: (\w+)").Groups[1].Value;}
    static RectangleF RectOf(string block,int height){var m=Regex.Match(block,@"(?s)      rect:\r?\n        serializedVersion: \d+\r?\n        x: ([\d.]+)\r?\n        y: ([\d.]+)\r?\n        width: ([\d.]+)\r?\n        height: ([\d.]+)");if(!m.Success)throw new Exception("Missing sprite rectangle");return new RectangleF(N(m.Groups[1].Value),height-N(m.Groups[2].Value)-N(m.Groups[4].Value),N(m.Groups[3].Value),N(m.Groups[4].Value));}
    static float[] BorderOf(string block){var m=Regex.Match(block,@"      border: \{x: ([\d.]+), y: ([\d.]+), z: ([\d.]+), w: ([\d.]+)\}");if(!m.Success)throw new Exception("Missing sprite border");return new[]{N(m.Groups[1].Value),N(m.Groups[2].Value),N(m.Groups[3].Value),N(m.Groups[4].Value)};}
    static string Block(string meta,string name){foreach(var block in Regex.Split(meta,@"(?m)(?=    - serializedVersion: 2)"))if(Regex.IsMatch(block,@"(?m)^      name: "+Regex.Escape(name)+@"\s*$"))return block;throw new Exception("Missing sprite "+name);}
    static Rectangle AlphaBounds(Bitmap b,Rectangle rect){int l=rect.Right,t=rect.Bottom,r=-1,bot=-1;for(int y=rect.Top;y<rect.Bottom;y++)for(int x=rect.Left;x<rect.Right;x++)if(b.GetPixel(x,y).A>24){l=Math.Min(l,x);r=Math.Max(r,x);t=Math.Min(t,y);bot=Math.Max(bot,y);}if(r<l)throw new Exception("Empty illustration");return Rectangle.FromLTRB(Math.Max(rect.Left,l-2),Math.Max(rect.Top,t-2),Math.Min(rect.Right,r+3),Math.Min(rect.Bottom,bot+3));}
    static Graphics Quality(Bitmap b){var g=Graphics.FromImage(b);g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;return g;}
    static void Copy(Graphics g,Bitmap b,RectangleF source,RectangleF dest){g.DrawImage(b,dest,source,GraphicsUnit.Pixel);}
    static void Nine(Graphics g,Bitmap b,RectangleF src,float[] sb,RectangleF dest,float[] db){float[] sx={src.Left,src.Left+sb[0],src.Right-sb[2],src.Right},sy={src.Top,src.Top+sb[3],src.Bottom-sb[1],src.Bottom};float[] dx={dest.Left,dest.Left+db[0],dest.Right-db[2],dest.Right},dy={dest.Top,dest.Top+db[3],dest.Bottom-db[1],dest.Bottom};for(int y=0;y<3;y++)for(int x=0;x<3;x++)Copy(g,b,new RectangleF(sx[x],sy[y],sx[x+1]-sx[x],sy[y+1]-sy[y]),new RectangleF(dx[x],dy[y],dx[x+1]-dx[x],dy[y+1]-dy[y]));}
    public static Result Run(string project,string stage)
    {
        string resources="Assets/Resources/DoodleLab/";
        var atlases=new[]{
            new Atlas{Role="Bar",Path=resources+"Map_Bars.png",CellWidth=512,CellHeight=128,Columns=2,Rows=4},
            new Atlas{Role="Mascot",Path=resources+"Map_Mascots.png",CellWidth=512,CellHeight=512,Columns=4,Rows=2},
            new Atlas{Role="MenuPaper",Path=resources+"Map_MenuPapers.png",CellWidth=768,CellHeight=1024,Columns=4,Rows=2},
            new Atlas{Role="Table",Path=resources+"Map_Tables.png",CellWidth=512,CellHeight=512,Columns=4,Rows=2},
            new Atlas{Role="Menu",Path=resources+"Map_Panels.png",CellWidth=512,CellHeight=512,Columns=4,Rows=2}
        };
        var changes=new List<Migration>();
        string template=File.ReadAllText(Path.Combine(project,resources,"Frames.png.meta"));
        string head=template.Substring(0,template.IndexOf("  spriteSheet:"));
        foreach(var atlas in atlases)
        {
            atlas.Guid=Id(atlas.Path);int width=atlas.CellWidth*atlas.Columns,height=atlas.CellHeight*atlas.Rows;
            using(var output=new Bitmap(width,height,PixelFormat.Format32bppArgb))using(var g=Quality(output))
            {
                g.Clear(Color.Transparent);
                string header=Regex.Replace(head,@"(?m)^guid: .*?$","guid: "+atlas.Guid);
                header=Regex.Replace(header,@"(?m)^  spritePixelsToUnits: .*?$","  spritePixelsToUnits: 100");
                var meta=new StringBuilder(header);meta.AppendLine("  spriteSheet:\n    serializedVersion: 2\n    sprites:");
                for(int i=0;i<8;i++)
                {
                    int x=i%atlas.Columns*atlas.CellWidth,y=i/atlas.Columns*atlas.CellHeight;
                    string name="Map_"+Themes[i]+"_"+atlas.Role,newId=(21300001+i).ToString(CultureInfo.InvariantCulture);
                    float[] border=atlas.Role=="Bar"?new float[]{176,48,96,48}:(atlas.Role=="Table"||atlas.Role=="Menu")?new float[]{144,144,144,144}:new float[4];
                    string source=Path.Combine(project,resources,"Map_"+Themes[i]+".png");
                    if(atlas.Role=="MenuPaper")source=Path.Combine(project,"Assets/4. SlimeAsset/Map/Menu/"+Themes[i]+"_Menu.png");
                    string sourceMeta=File.ReadAllText(source+".meta");
                    using(var art=new Bitmap(source))
                    {
                        RectangleF rect;string oldId;
                        if(atlas.Role=="MenuPaper")
                        {
                            bool multiple=Regex.IsMatch(sourceMeta,@"(?m)^  spriteMode: 2\s*$");
                            rect=multiple?RectOf(sourceMeta,art.Height):new RectangleF(0,0,art.Width,art.Height);
                            oldId=multiple?Regex.Match(sourceMeta,@"(?m)^      internalID: (-?\d+)").Groups[1].Value:"21300000";
                            rect=AlphaBounds(art,Rectangle.Round(rect));
                        }
                        else
                        {
                            string block=Block(sourceMeta,name);rect=RectOf(block,art.Height);
                            oldId=Regex.Match(block,@"(?m)^      internalID: (-?\d+)").Groups[1].Value;
                            if(atlas.Role=="Bar"||atlas.Role=="Table"||atlas.Role=="Menu")
                            {
                                int pad=atlas.Role=="Bar"?4:8;
                                var dest=new RectangleF(x+pad,y+pad,atlas.CellWidth-2*pad,atlas.CellHeight-2*pad);
                                var adjusted=new[]{border[0]-pad,border[1]-pad,border[2]-pad,border[3]-pad};
                                Nine(g,art,rect,BorderOf(block),dest,adjusted);
                            }
                        }
                        if(atlas.Role=="Mascot"||atlas.Role=="MenuPaper")
                        {
                            int pad=16;float scale=Math.Min((atlas.CellWidth-2f*pad)/rect.Width,(atlas.CellHeight-2f*pad)/rect.Height);
                            float w=rect.Width*scale,h=rect.Height*scale,top=atlas.Role=="Mascot"?y+atlas.CellHeight-pad-h:y+(atlas.CellHeight-h)/2f;
                            Copy(g,art,rect,new RectangleF(x+(atlas.CellWidth-w)/2f,top,w,h));
                        }
                        changes.Add(new Migration{OldGuid=GuidOf(sourceMeta),OldId=oldId,NewGuid=atlas.Guid,NewId=newId,Name=name});
                    }
                    meta.AppendLine("    - serializedVersion: 2\n      name: "+name+"\n      rect:\n        serializedVersion: 2\n        x: "+x+"\n        y: "+(height-y-atlas.CellHeight)+"\n        width: "+atlas.CellWidth+"\n        height: "+atlas.CellHeight+"\n      alignment: 0\n      pivot: {x: 0.5, y: 0.5}\n      border: {x: "+F(border[0])+", y: "+F(border[1])+", z: "+F(border[2])+", w: "+F(border[3])+"}\n      customData: \n      outline: []\n      physicsShape: []\n      tessellationDetail: -1\n      bones: []\n      spriteID: "+Id(name)+"\n      internalID: "+newId+"\n      vertices: []\n      indices: \n      edges: []\n      weights: []");
                }
                meta.AppendLine("    outline: []\n    customData: \n    physicsShape: []\n    bones: []\n    spriteID: "+Id(atlas.Path+"sheet")+"\n    internalID: 0\n    vertices: []\n    indices: \n    edges: []\n    weights: []\n    secondaryTextures: []\n    nameFileIdTable:");
                for(int i=0;i<8;i++)meta.AppendLine("      Map_"+Themes[i]+"_"+atlas.Role+": "+(21300001+i));
                meta.AppendLine("  userData: Role atlas; ordered Basic Spring Summer Autumn Winter Sunny Night Cloudy\n  assetBundleName: \n  assetBundleVariant: ");
                string path=Path.Combine(stage,"payload",atlas.Path);Directory.CreateDirectory(Path.GetDirectoryName(path));
                output.Save(path,ImageFormat.Png);File.WriteAllText(path+".meta",meta.ToString(),new UTF8Encoding(false));
            }
        }
        return new Result{Atlases=atlases,References=changes.ToArray()};
    }
}
