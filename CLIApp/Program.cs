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
            if(args.Length != 1)
            {
                Console.WriteLine("usage: cliapp <lego-project-file>");
                return;
            }
            using (Stream stream = File.OpenRead(args[0]))
            {
                //try
                //{
                    //SB3NodesPrinter.PrintProgram(target_blocks.Value<JObject>());

                    //(var stream_out, var name_out) = LegoAppToolsLib.LegoAppTools.RepairFile(stream, file, true);
                    //var result = LegoAppToolsLib.LegoAppTools.GeneratePngCanvas(stream, file);

                    //stream.Position = 0;
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
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine(ex.Message);
                //}
            }
        }
    }
}
