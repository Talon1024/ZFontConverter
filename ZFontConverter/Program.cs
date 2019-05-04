using System;
using System.Text;
// using System.Collections.Generic;
using ZFontConverter.GUI;

namespace ZFontConverter {
    class MainClass
    {
        public static void Main(string[] args)
        {
            bool setCodePage = false;
            if (args.Length > 0)
            {
                // List<string> wads = new List<string>();
                foreach (string arg in args)
                {
                    if (arg == "--codepage")
                    {
                        setCodePage = true;
                        continue;
                    }
                    if (setCodePage)
                    {
                        FontProcessing.codePage = Encoding.GetEncoding(arg);
                        continue;
                    }

                    /*
                    if (IsWad(arg))
                    {
                        wads.Add(arg);
                        continue;
                    }
                    */

                    try
                    {
                        FontProcessing.ConvertFont(arg);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"{arg} cannot be converted: {ex}");
                    }
                }
            }
            else
            {
                /*
                // Help
                Console.Write(
                    "ZFontConverter 0.1 by Kevin Caccamo\n" +
                    "Converts FON2 and BMF fonts to GZDoom Unicode fonts\n" +
                    "\n" +
                    "Usage:\n" +
                    "ZFontConverter.exe [--codepage <encoding>] font...\n" +
                    "\n" +
                    "Default codepage is iso-8859-1\n" +
                    "See https://docs.microsoft.com/en-us/dotnet/api/system.text.encoding?view=netframework-4.7.2 for a list of supported encodings and their names.\n"
                );
                */
                MainWindow mainWindow = new MainWindow();
                mainWindow.ShowDialog();
            }
        }

        public static bool IsWad(string fname)
        {
            return fname.ToLower().EndsWith("wad", StringComparison.Ordinal);
        }
    }
}
