using System.IO;
using System.Reflection;

namespace Cet.NATS.Client.DemoShared
{
    public class CommonUtils
    {

        public const string SubjectAscii = "mysubject";
        public const string SubjectUnicode = "Растения";

        public const string SampleTextAscii = "Recent developments on the world political stage have brought the destructive potential of electromagnetic pulses (EMP) to the fore, and people seem to have internalized the threat posed by a single thermonuclear weapon.";
        public const string SampleTextUnicode = "植物という語が指し示す範囲は歴史的に変遷してきており、現在でも複数の定義が並立している。";


        static CommonUtils()
        {
            string path = Path.Combine(
                Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                "resources"
                );

            SampleText_10 = File.ReadAllText(Path.Combine(path, "SampleTextFile_10b.txt"));
            SampleText_100 = File.ReadAllText(Path.Combine(path, "SampleTextFile_100b.txt"));
            SampleText_1000 = File.ReadAllText(Path.Combine(path, "SampleTextFile_1kb.txt"));
            SampleText_10k = File.ReadAllText(Path.Combine(path, "SampleTextFile_10kb.txt"));
            SampleText_100k = File.ReadAllText(Path.Combine(path, "SampleTextFile_100kb.txt"));
            SampleText_1M = File.ReadAllText(Path.Combine(path, "SampleTextFile_1E6b.txt"));
            SampleText_1000k = File.ReadAllText(Path.Combine(path, "SampleTextFile_1000kb.txt"));
            SampleText_6M = File.ReadAllText(Path.Combine(path, "SampleTextFile_6Mb.txt"));
        }


        public static readonly string SampleText_10;
        public static readonly string SampleText_100;
        public static readonly string SampleText_1000;
        public static readonly string SampleText_10k;
        public static readonly string SampleText_100k;
        public static readonly string SampleText_1M;
        public static readonly string SampleText_1000k;
        public static readonly string SampleText_6M;

    }
}
