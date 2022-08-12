using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Svg;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace LegoAppToolsLib
{
    using LegoAppStatsList = Dictionary<string, string>;
    using LegoAppErrorList = List<string>;
    using LegoAppCodeListing = List<string>;

    public readonly struct StreamOutStruct
    {
        public StreamOutStruct(string name_, Stream stream_)
        {
            stream_.Position = 0;
            this.name = name_; this.stream = stream_;
        }
        public readonly string name;
        public readonly Stream stream;
    }

    public class LegoAppTools
    {
        private const string FN_MANIFEST = "manifest.json";
        private const string FN_ICONSVG = "icon.svg";

        /// <summary>
        /// Retrieve the stream of the SVG entry from a LEGO file
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        /// <exception cref="LegoAppToolException"></exception>
        static public Stream GetSVGStreamFromLegoFileStream(Stream stream)
        {
            MemoryStream stream_out = new MemoryStream(); //-- do not dispose, as it needs to be returned

            //-- open zip in zip structure
            using var zip1 = new ZipFile(stream);
            ZipEntry ze1 = zip1.GetEntry(FN_ICONSVG);
            if (ze1 == null) throw new LegoAppToolException("#MISSVG Invalid LEGO content file.");

            using Stream stream1 = zip1.GetInputStream(ze1);
            stream1.CopyTo(stream_out);
            stream_out.Position = 0;

            return stream_out;
        }

        /// <summary>
        /// Get SVGDoc from a LEGO file strea,
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        static private SvgDocument GetSVGDocFromLegoFileStream(Stream stream)
        {
            using var stream1 = GetSVGStreamFromLegoFileStream(stream);
            stream1.Position = 0;
            var svgDocument = SvgDocument.Open<SvgDocument>(stream1);
            return svgDocument;
        }

        /// <summary>
        /// Generate a PNG file from a LEGO file
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        /// <exception cref="LegoAppToolException"></exception>
        static public StreamOutStruct GeneratePngCanvas(Stream stream, string filename)
        {
            MemoryStream stream_out = new MemoryStream(); //-- do not dispose, as it needs to be returned
            var svgDocument = GetSVGDocFromLegoFileStream(stream);

            //-- need to reformat a few nodes, incomplete css compatibility: "important" is disregarded unfortunately
            var cswhite = new SvgColourServer(Color.White);
            void processNodes(IEnumerable<SvgElement> nodes)
            {
                foreach (var node in nodes)
                {
                    //if (node.Fill != SvgPaintServer.None) node.Fill = colorServer;
                    //if (node.Color != SvgPaintServer.None) node.Color = colorServer;
                    //if (node.StopColor != SvgPaintServer.None) node.StopColor = colorServer;
                    //if (node.Stroke != SvgPaintServer.None) node.Stroke = colorServer;

                    if (node is SvgText)
                    {
                        var x1 = node as SvgText;
                        if (x1.CustomAttributes.TryGetValue("class", out string classvalue))
                        {
                            if (classvalue.Contains("blocklyDropdownText")) x1.Fill = cswhite;
                            x1.Y[0] = x1.Y[0].Value + 3.5f;
                        }
                    }

                    if (node.HasChildren()) processNodes(node.Children);
                }
            }
            processNodes(svgDocument.Children);

            int targetwidth = (int)(svgDocument.Bounds.Width * 2);
            var bitmap = svgDocument.Draw(targetwidth, 0);

            //IDEA: add project xml as an additional chunk

            bitmap.Save(stream_out, ImageFormat.Png);
            stream_out.Position = 0;

            string filename_out = Path.GetFileNameWithoutExtension(filename) + "_canvas" + ".png";
            return new StreamOutStruct(filename_out, stream_out);

            throw new LegoAppToolException("#BADFILE Invalid file.");
        }

        /// <summary>
        /// Get LEGO file contents
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        /// <exception cref="LegoAppToolException"></exception>
        static public (LegoAppCodeListing code, LegoAppStatsList stats) GetFileContents(Stream stream)
        {
            using ZipFile zip1 = _GetLegoFile(stream, out JObject manifest, out string program_type);
            LegoAppCodeListing code;
            LegoAppStatsList stats;
            LegoAppErrorList errors;

            if (program_type == "word-blocks" || program_type == "icon-blocks")
            {
                (code, stats, errors) = Scratch3FilePrinter.GetProgram(zip1, program_type == "word-blocks", manifest);
            }
            else if (program_type == "python")
            {
                (stats, errors) = Generic_GetFileStats(zip1, manifest);
                code = PythonFilePrinter.GetProgramContents(zip1);
            }
            else
            {
                throw new LegoAppToolException("#ERRTYPE Invalid file.");
            }

            //-- add errors to stats
            if (errors.Count > 0) stats["errors"] = string.Join("\r\n", errors.ToArray());

            //-- insert header to the top
            try
            {
                var header = new List<string>();
                header.Add(String.Empty);
                header.Add($"\"\"\"" + $"Program '{stats["name"]}'");
                if (stats.ContainsKey("type")) header.Add($"   program type '{stats["type"]}'");
                if (stats.ContainsKey("slot")) header.Add($"   in slot {stats["slot"]}");
                if (stats.ContainsKey("hub")) header.Add($"   running on {stats["hub"]} hub");
                header.Add($"\"\"\"");
                header.Add(String.Empty);
                code.InsertRange(0, header);
            }
            catch { }

            return (code, stats);
        }

        /// <summary>
        /// Generic Stats generator of a LEGO file
        /// </summary>
        /// <param name="zip1"></param>
        /// <param name="manifest"></param>
        /// <returns></returns>
        static internal (LegoAppStatsList retval, LegoAppErrorList errors) Generic_GetFileStats(ZipFile zip1, JObject manifest)
        {
            LegoAppErrorList errors = new LegoAppErrorList();

            var retval = new LegoAppStatsList();
            try
            {
                retval["name"] = manifest.GetValue("name")?.ToString();
                retval["slot"] = manifest.GetValue("slotIndex")?.ToString();
                var hw0 = (manifest.GetValue("hardware")?.FirstOrDefault() as JProperty)?.Value as JObject;
                if (hw0 != null)
                {
                    JToken name;
                    if (!hw0.TryGetValue("name", out name)) hw0.TryGetValue("advertisedName", out name);
                    if (name != null) retval["hub"] = name.ToString();

                    JToken connection = hw0.GetValue("connection");
                    if (connection != null) retval["connection"] = connection.ToString();
                }
                retval["type"] = manifest.GetValue("type")?.ToString();
                retval["created"] = manifest.GetValue("created")?.ToString();
                retval["last saved"] = manifest.GetValue("lastsaved")?.ToString();
            }
            catch { }

            return (retval, errors);
        }


        /// <summary>
        /// Repair a damaged LEGO file
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="filename"></param>
        /// <param name="selectedpart_is_first"></param>
        /// <returns></returns>
        static public StreamOutStruct RepairFile(Stream stream, string filename, bool selectedpart_is_first)
        {
            using ZipFile zip1 = _GetLegoFile(stream, out JObject manifest, out _);

            return Scratch3FileUtils.RepairFile(zip1, filename, selectedpart_is_first);
        }

        /// <summary>
        /// Open a LegoFile
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="manifest"></param>
        /// <returns></returns>
        /// <exception cref="LegoAppToolException"></exception>
        private static ZipFile _GetLegoFile(Stream stream, out JObject manifest, out string program_type)
        {
            //-- open zip in zip structure
            /*using*/
            ZipFile zip1 = new ZipFile(stream, true);

            //-----------------------------------------
            //-- process MANIFEST
            ZipEntry ze1_manifest = zip1.GetEntry(FN_MANIFEST);
            if (ze1_manifest == null) throw new LegoAppToolException("#MISMNF Invalid LEGO content file");
            using (StreamReader reader = new StreamReader(zip1.GetInputStream(ze1_manifest)))
            using (JsonTextReader jsonReader = new JsonTextReader(reader))
            {
                JsonSerializer ser = new JsonSerializer();
                manifest = ser.Deserialize<JObject>(jsonReader);
            }
            program_type = manifest.GetValue("type").ToString();

            return zip1;
        }
    }
}
