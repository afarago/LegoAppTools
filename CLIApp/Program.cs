using LegoAppToolsLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CLIApp
{
    using LegoAppStatsList = Dictionary<string, string>;
    using LegoAppErrorList = List<string>;
    using LegoAppCodeListing = List<string>;

    class Program
    {
        static void Main(string[] args)
        {
            //string file = @"e:\FirstLegoLeague\z_Fixing\Eva\take1\program.llsp";
            //string file = @"c:\Users\attil\Documents\LEGO MINDSTORMS\WRO22_Harry_1.lms";

            string file =
            //@"C:\Users\attil\Documents\LEGO MINDSTORMS\NLF_test2.lms";
            //@"c:\Users\attil\Documents\LEGO Education SPIKE\python test1.llsp";
            //@"C:\Users\attil\Documents\LEGO Education SPIKE\NLF_test1.llsp";
            //@"c:\Users\attil\Documents\LEGO Education SPIKE\aprilis28.llsp";
            //@"c:\Users\attil\Documents\LEGO Education SPIKE\CSAL3F WRO.llsp";
            //@"e:\FirstLegoLeague\z_Fixing\TertschAgi\DO_NOT_TOUCH_MaGaBen.llsp";
            //@"c:\Users\attil\Documents\LEGO Education SPIKE\iconblocks_test2.llsp";
            //@"c:\Users\attil\Documents\LEGO Education SPIKE\essential_testicon.llsp";
            //@"e:\FirstLegoLeague\2022_WRO_Jovobelatok\programs\WRO22_Luna_2.lms";
            @"e:\FirstLegoLeague\z_Fixing\Ravi\BLACK-1a-1.lmsp";
            using (Stream stream = File.OpenRead(file))
            {
                try
                {
                    //SB3NodesPrinter.PrintProgram(target_blocks.Value<JObject>());

                    //(var stream_out, var name_out) = LegoAppToolsLib.LegoAppTools.RepairFile(stream, file, true);
                    //var result = LegoAppToolsLib.LegoAppTools.GeneratePngCanvas(stream, file);

                    stream.Position = 0;
                    (LegoAppCodeListing code, LegoAppStatsList stats) = LegoAppTools.GetFileContents(stream);
                    Console.WriteLine(string.Join("\r\n", code.ToArray()));
                    Console.WriteLine(string.Join("\r\n", stats.Select(kvp => $"{kvp.Key} = {kvp.Value}").ToArray()));

                    //string filename_out = Path.Combine(Path.GetDirectoryName(file), result.name);
                    //using (var stream_out_fs = File.Create(filename_out))
                    //{
                    //    result.stream.Position = 0;
                    //    result.stream.CopyTo(stream_out_fs);
                    //}

                    //Console.WriteLine(filename_out);

                    ////Process.Start(filename_out);
                    //System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo(filename_out);
                    //info.RedirectStandardError = false;
                    //info.RedirectStandardOutput = false;
                    //info.UseShellExecute = true;
                    //System.Diagnostics.Process p = new System.Diagnostics.Process();
                    //p.StartInfo = info;
                    //p.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
