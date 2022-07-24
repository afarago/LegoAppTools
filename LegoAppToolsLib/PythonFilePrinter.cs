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
    using LegoAppStatsList = Dictionary<string, string>;
    using LegoAppErrorList = List<string>;
    using LegoAppCodeListing = List<string>;

    internal class PythonFilePrinter
    {
        private const string FN_PROJECTBODY = "projectbody.json";

        static public LegoAppCodeListing GetProgramContents(ZipFile zip1)
        {
            //projectbody.json
            ZipEntry ze1_projectbody = zip1.GetEntry(FN_PROJECTBODY);
            if (ze1_projectbody == null) throw new LegoAppToolException("#MISPBODY Invalid LEGO content file");

            using (StreamReader reader = new StreamReader(zip1.GetInputStream(ze1_projectbody)))
            using (JsonTextReader jsonReader = new JsonTextReader(reader))
            {
                JsonSerializer ser = new JsonSerializer();
                JObject jo_main = ser.Deserialize<JObject>(jsonReader);

                //-- retrieve main node
                if (!jo_main.TryGetValue("main", out JToken jt) || !(jt is JValue))
                    throw new LegoAppToolException("#MISPYMAIN Invalid LEGO content file");
                string contents = jt.ToString();

                return (LegoAppCodeListing)contents.Split("\n").ToList();
            }
        }

    }
}