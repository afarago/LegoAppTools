using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LegoAppToolsLib
{
    internal class Scratch3FileUtils
    {
        private const string FN_PROJECT = "project.json";
        private const string FN_SCRATCHSB3 = "scratch.sb3";

        internal static (MemoryStream stream2, ZipFile zip2) _GetLegoFileProjectJSON(ZipFile zip1, out JObject project, out JArray project_targets)
        {
            //if (prg_type=="python")
            //word-blocks, icon-blocks

            //-----------------------------------------
            //-- process SCRATCH.SB3 (ZIP2) and PROJECT
            ZipEntry ze1_scratchsb3 = zip1.GetEntry(FN_SCRATCHSB3);
            if (ze1_scratchsb3 == null) throw new LegoAppToolException("#MISSB3 Invalid LEGO content file");

            //-- copy to memory stream to provide a seekable stream for ZIP2
            /*using*/
            MemoryStream stream2 = new MemoryStream();
            zip1.GetInputStream(ze1_scratchsb3).CopyTo(stream2);
            /*using*/
            ZipFile zip2 = new ZipFile(stream2);
            ZipEntry ze2 = zip2.GetEntry(FN_PROJECT);
            if (ze2 == null) throw new LegoAppToolException("#ERRMISPROJ Invalid LEGO content file");

            using (StreamReader reader = new StreamReader(zip2.GetInputStream(ze2)))
            using (JsonTextReader jsonReader = new JsonTextReader(reader))
            {
                JsonSerializer ser = new JsonSerializer();
                /*JObject*/
                project = ser.Deserialize<JObject>(jsonReader);

                //-- retrieve targets node
                if (!project.TryGetValue("targets", out JToken jt) || !(jt is JArray))
                    throw new LegoAppToolException("#ERRMISTRG Invalid LEGO content file");
                project_targets = jt as JArray;
            }

            return (stream2, zip2);
        }

        static public StreamOutStruct RepairFile(ZipFile zip1, string filename, bool selectedpart_is_first)
        {
            var disposables = Scratch3FileUtils._GetLegoFileProjectJSON(zip1, out JObject project, out JArray project_targets);
            using MemoryStream stream2 = disposables.stream2;
            using ZipFile zip2 = disposables.zip2;

            //-- validity checks
            if (project_targets.Count == 2) throw new LegoAppToolException("This is a valid LEGO Content file");
            if (project_targets.Count != 4) throw new LegoAppToolException("#ERRCNT Invalid LEGO content file");
            if (!project_targets[0].Value<bool>("isStage") && project_targets[1].Value<bool>("isStage")) throw new LegoAppToolException("#ERRSTG1 Invalid LEGO content file");
            if (!project_targets[2].Value<bool>("isStage") && project_targets[3].Value<bool>("isStage")) throw new LegoAppToolException("#ERRSTG2 Invalid LEGO content file");

            //-- remove extra nodes
            for (int kr = 0; kr < 2; kr++)
            {
                if (selectedpart_is_first) project_targets.RemoveAt(2); // remove second ones
                else project_targets.RemoveAt(0); // remove first pair
            }

            //-- repack JSON project
            string res_project = JsonConvert.SerializeObject(project);

            //-- re-assemble zip2
            MemoryStream zipOutMemoryStream1 = new MemoryStream(); //-- do not dispose, as it needs to be returned
            using (ZipOutputStream zipStream1 = new ZipOutputStream(zipOutMemoryStream1))
            {
                zipStream1.SetLevel(9);
                zipStream1.IsStreamOwner = false;

                //-- ZIP1 copy all non scratch.sb3 files, repack ZIP1
                foreach (var ze1_in in zip1.Cast<ZipEntry>().Where(ze => ze.Name != FN_SCRATCHSB3))
                {
                    ZipEntry ze1_out = new ZipEntry(ze1_in.Name);
                    zipStream1.PutNextEntry(ze1_out);
                    ze1_out.Size = ze1_in.Size;
                    zip1.GetInputStream(ze1_in).CopyTo(zipStream1);
                    zipStream1.CloseEntry();
                }

                //-- ZIP2 modify project.json file, copy others, repack ZIP2, include in ZIP1
                using (MemoryStream zipOutMemoryStream2 = new MemoryStream())
                using (var zipStream2 = new ZipOutputStream(zipOutMemoryStream2))
                {
                    zipStream2.SetLevel(9);
                    //zipStream2.IsStreamOwner = false;

                    foreach (ZipEntry ze2_in in zip2.Cast<ZipEntry>())
                    {
                        ZipEntry ze2_out = new ZipEntry(ze2_in.Name);

                        zipStream2.PutNextEntry(ze2_out);
                        if (ze2_in.Name == FN_PROJECT)
                        {
                            using (var sw = new StreamWriter(zipStream2, null, -1, true))
                                sw.Write(res_project);
                        }
                        else
                        {
                            ze2_out.Size = ze2_in.Size;
                            zip2.GetInputStream(ze2_in).CopyTo(zipStream2);
                        }
                        zipStream2.CloseEntry();
                    }

                    //-- finish off ZIP2 streams
                    zipStream2.Finish();

                    //-- add modified ZIP2 to ZIP1
                    ZipEntry ze1_out = new ZipEntry(FN_SCRATCHSB3);
                    zipStream1.PutNextEntry(ze1_out);
                    ze1_out.Size = zipOutMemoryStream2.Length;
                    zipOutMemoryStream2.Position = 0;
                    zipOutMemoryStream2.CopyTo(zipStream1);
                    zipStream1.CloseEntry();
                }
                //-- finish off ZIP1 streams
                zipStream1.Finish();

                zipOutMemoryStream1.Position = 0;
                string filename_out =
                    Path.GetFileNameWithoutExtension(filename) +
                    "_fixed_" + (selectedpart_is_first ? "first" : "second") +
                    Path.GetExtension(filename);

                return new StreamOutStruct(filename_out, zipOutMemoryStream1);
            }
        }
    }
}
