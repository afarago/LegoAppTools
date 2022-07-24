//#define PRINT_UNKNOWN_TRANSLATIONS

using System.Linq;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace LegoAppToolsLib
{
    using LegoAppStatsList = Dictionary<string, string>;
    using LegoAppErrorList = List<string>;
    using LegoAppCodeListing = List<string>;

    internal partial class Scratch3FilePrinter
    {
        /// <summary>
        /// Retrieve LEGO file code and stats
        /// </summary>
        /// <param name="zip1"></param>
        /// <param name="is_wordblocks"></param>
        /// <param name="manifest"></param>
        /// <returns></returns>
        static public (LegoAppCodeListing code, LegoAppStatsList stats, LegoAppErrorList errors) GetProgram(ZipFile zip1, bool is_wordblocks, JObject manifest)
        {
            var disposables = Scratch3FileUtils._GetLegoFileProjectJSON(zip1, out JObject project, out JArray project_targets);
            using MemoryStream stream2 = disposables.stream2;
            using ZipFile zip2 = disposables.zip2;

            //-- get statistics
            (LegoAppStatsList stats, LegoAppErrorList errors) = _GetFileStats(zip1, manifest, project, project_targets);

            //-- get listings
            LegoAppCodeListing code_listing = (LegoAppCodeListing)(_GetProgramContents(zip1, project, project_targets, is_wordblocks).ToList());

            return (code_listing, stats, errors);
        }

        /// <summary>
        /// Retrieve LEGO file app stats
        /// </summary>
        /// <param name="zip1"></param>
        /// <param name="manifest"></param>
        /// <param name="project"></param>
        /// <param name="project_targets"></param>
        /// <returns></returns>
        static public (LegoAppStatsList stats, LegoAppErrorList errors) _GetFileStats(ZipFile zip1,
            JObject manifest,
            JObject project, JArray project_targets)
        {
            (LegoAppStatsList retval, LegoAppErrorList errors) = LegoAppTools.Generic_GetFileStats(zip1, manifest);

            if (project_targets.Count != 2)
                errors.Add($"#ERRTRG1 Invalid LEGO content file, mismatching project target count ({project_targets.Count})");
            if (!(project_targets[0].Value<bool>("isStage") && !project_targets[1].Value<bool>("isStage")))
                errors.Add($"#ERRTRG2 Invalid LEGO content file, mismatching isStage values ({project_targets[0].Value<bool>("isStage")}, {project_targets[1].Value<bool>("isStage")})");

            var target_prg = project_targets[1] as JObject; //non-stage
            var target_blocks = target_prg["blocks"];

            int num_blocks = target_blocks.Children().Count();
            int blocks_toplevel = 0;
            int blocks_hat = 0;
            foreach (JProperty blockp in target_blocks)
            {
                JObject block = blockp.Value as JObject; if (block == null) continue;
                block.TryGetValue("parent", out JToken prop_parent);
                block.TryGetValue("opcode", out JToken prop_opcode);
                block.TryGetValue("toplevel", out JToken prop_toplevel);
                if (prop_parent.Value<string>() == null)
                {
                    blocks_toplevel++;
                    if (Scratch3FilePrinter.IsHatBlock(prop_opcode.ToString()))
                    {
                        blocks_hat++;
                    }
                    else
                    {
                        //mutation
                        string block_name = prop_opcode.ToString();
                        if (block.TryGetValue("mutation", out JToken prop_mutation))
                        {
                            if (prop_mutation.Value<JObject>().TryGetValue("proccode", out JToken prop_proccode))
                            {
                                block_name += "<" + prop_proccode.Value<string>() + ">";
                            }
                        }

                        //inputs
                        var block_inputs = new Dictionary<string, string>();
                        if (block.TryGetValue("inputs", out JToken prop_inputs))
                        {
                            foreach (var input in prop_inputs.Value<JObject>())
                            {
                                var values = input.Value as JArray;
                                if (values == null || values.Count < 2) continue;
                                if (values[0].ToString() == "1")
                                {
                                    if (values[1] is JArray)
                                    {
                                        //-- direct value
                                        var valuesvalues = values[1] as JArray;
                                        var valuetype = valuesvalues[0].ToString();
                                        if (valuetype == "4" || valuetype == "10")
                                        {
                                            // actual value
                                            block_inputs[input.Key] = valuesvalues[1].ToString();
                                        }
                                    }
                                    else if (values[1] is JValue && values[1].Value<string>().Length == 20)
                                    {
                                        //-- node reference like "nwqMZhP}|kyhXrODU#NZ"

                                        var noderefid = values[1].Value<string>();
                                        var refnode = target_blocks[noderefid];
                                    }
                                }
                            }

                            string sparams = string.Join(", ",
                                block_inputs.Select(kvp => $"{(kvp.Key.ToUpper() == kvp.Key ? kvp.Key + "= " : null)}{kvp.Value}").ToArray());
                            errors.Add($"#WARNLONESTK1 Lonely stack {block_name}({sparams})"); // @{blockp.Name}
                        }
                    }
                }
            }

            retval["number of blocks"] = num_blocks.ToString();
            retval["number of hats"] = blocks_hat.ToString();

            return (retval, errors);
        }

        /// <summary>
        /// Get Program contents
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        static private IEnumerable<string> _GetProgramContents(ZipFile zip1, JObject project, JArray project_targets,
            bool is_wordblocks, bool addheader = true)
        {
            var target_prg = project_targets[1] as JObject; //non-stage
            var blocks_root = target_prg["blocks"].Value<JObject>();
            var comments_root = target_prg["comments"].Value<JObject>();
            var nodes = blocks_root.Cast<JProperty>().ToList();

            var toplevels1 = nodes
                .Where(elem => ((elem.Value as JObject)?.GetValue(SB3PROP_TOPLEVEL)?.Value<bool>() == true))
                .Select(elem =>
                {
                    var node = elem.Value as JObject;
                    var x = node.GetValue("x").Value<int>();
                    var y = node.GetValue("y").Value<int>();
                    return new { prop = elem, x = x, y = y };
                    //return new Tuple<JProperty, int, int>(elem, x, y);
                }).ToList();
            ////-- sort by X: (X-x_min)\X_OFFSET then by Y
            //.OrderBy(elem => Math.Truncate((double)(elem.x - minx) / X_OFFSET + 0.2))
            //.ThenBy(elem => elem.y);
            var toplevels2 = toplevels1.Take(0).ToList();

            while (toplevels1.Count() > 0)
            {
                int current_primarydim = toplevels1.Min(elem => is_wordblocks ? elem.x : elem.y);
                int current_column_max = current_primarydim + (int)Math.Round(1.2 * (is_wordblocks ? X_OFFSET_WORDBLOCKS : Y_OFFSET_ICONBLOCKS));
                var selecteditems = toplevels1.Where(elem => (is_wordblocks ? elem.x : elem.y) <= current_column_max)
                    .OrderBy(elem => is_wordblocks ? elem.y : elem.x)
                    .ToList();

                // add to result list (2), and remove from temp one (1)
                toplevels2.AddRange(selecteditems);
                selecteditems.ForEach(elem => toplevels1.Remove(elem));

                //TODO: leftmost item in the remainin stac
                // update current_x with new column max
            }

            //-- get all stack contents
            foreach (var tup3 in toplevels2)
            {
                JProperty stackroot = tup3.prop;
                var stack = GetStackContents(stackroot);
                if (stack != null)
                {
                    bool anyitems = false;
                    foreach (string item in stack)
                    {
                        anyitems = true;
                        yield return item;
                    }
                    if (anyitems) yield return string.Empty;
                }
            }
        }

        /// <summary>
        /// Print a Stack contents
        /// </summary>
        /// <param name="stackroot"></param>
        /// <param name="toplevel_stack"></param>
        /// <returns></returns>
        static private IEnumerable<string> GetStackContents(JProperty stackroot, bool toplevel_stack = true)
        {
            JObject current_node = stackroot.Value as JObject;
            bool isfirst = true;
            bool active_toplevel_stack = true;
            bool is_indenting_first = toplevel_stack;
            bool first_is_header = toplevel_stack;
            JObject blockroot = current_node.Parent.Parent as JObject; //-- jobject(current)->jproperty(current)->jobject(blocks)

            while (current_node != null)
            {
                List<string> retval1 = GetNodeContents(current_node, out string opcode, isfirst, false);

                if (toplevel_stack && isfirst && !IsHatBlock(opcode))
                {
                    yield return "# WARNING: Lonely inactive stack";
                    active_toplevel_stack = false;
                }

                foreach (string line in retval1)
                {
                    bool iscomment = line.StartsWith(COMMENT_PREFIX) || string.IsNullOrEmpty(line);
                    yield return
                        (!active_toplevel_stack ? COMMENT_PREFIX : null) +
                        (!isfirst && is_indenting_first && active_toplevel_stack ? INDENT_PREFIX : null) +
                        (line) +
                        (isfirst && first_is_header && active_toplevel_stack && !iscomment ? ":" : null);

                    if (!iscomment) isfirst = false;
                }

                if (current_node.TryGetValue(SB3PROP_NEXT, out JToken prop_next))
                {
                    var pn = prop_next.ToString();
                    //!! current_node = blocks_root.GetValue(pn) as JObject;
                    current_node = blockroot.GetValue(pn) as JObject;
                }
            }
        }

        /// <summary>
        /// Is this a Hat block
        /// </summary>
        /// <param name="opcode"></param>
        /// <returns></returns>
        internal static bool IsHatBlock(string opcode)
        {
            return opcode.EndsWith("Hat") ||
                                opcode.Equals("event_whenbroadcastreceived") ||
                                opcode.Equals("event_whenkeypressed") ||
                                opcode.StartsWith("flipperevents_") ||
                                opcode.StartsWith("horizontalevents_") ||
                                opcode.StartsWith("ev3events_") ||
                                opcode.StartsWith("flipperxboxgamepad_dpadWhen") ||
                                Regex.IsMatch(opcode, @"flipper\w+_\w+When") ||
                                opcode == OPCODE_PROCDEF;
        }

        /// <summary>
        /// Input Value type for a property
        /// </summary>
        private class InputValue
        {
            public InputValue(string value, string valueType)
            {
                Value = value;
                ValueType = valueType;
            }
            public string Value { get; set; }
            public string ValueType { get; set; }
        }

        /// <summary>
        /// Print a Node contents
        /// </summary>
        /// <param name="node"></param>
        /// <param name="opcode"></param>
        /// <returns></returns>
        private static List<string> GetNodeContents(JObject node, out string opcode, bool first_node, bool is_embedded)
        {
            JObject blockroot = node.Parent.Parent as JObject; //-- jobject(current)->jproperty(current)->jobject(blocks)

            List<string> retval = new List<string>();
            // https://en.scratch-wiki.info/wiki/Scratch_File_Format

            node.TryGetValue(SB3PROP_PARENT, out JToken prop_parent);
            node.TryGetValue(SB3PROP_OPCODE, out JToken prop_opcode);
            opcode = prop_opcode.ToString();
            string block_name = opcode.ToString();
            bool isownerblock = false;

            //-- get comment - for non embedded blocks only
            if (node.TryGetValue(SB3PROP_COMMENT, out JToken prop_comment))
            {
                string noderefid = prop_comment.Value<string>();
                var comments_root = node.Parent.Parent.Parent.Parent["comments"].Value<JObject>();
                var comments = comments_root.Value<JObject>();
                JProperty refnode = comments.Property(noderefid);
                var reference_prop_text = (refnode.Value as JObject).Property("text");
                var comment = reference_prop_text.Value.ToString();
                if (!string.IsNullOrEmpty(comment))
                {
                    //-- add extra line if not first (hat) block
                    if (!first_node) retval.Add(String.Empty);
                    comment.Split("\n").ToList().ForEach(
                        line => retval.Add(COMMENT_PREFIX + line.TrimEnd()));
                }
            }

            //-- add block header/name placeholder
            int idx_blockname_placeholder = retval.Count;
            retval.Add(opcode);

            //-- procedure call => rename block
            if (opcode == OPCODE_PROCCALL)
            {
                if (node.TryGetValue(SB3PROP_MUTATION, out JToken prop_mutation))
                    if (prop_mutation.Value<JObject>().TryGetValue(SB3PROP_PROCCODE, out JToken prop_proccode))
                    {
                        block_name = "call " + _ExtractFunctionName(prop_proccode, out _);
                        //block_name += _ExtractFunctionName(prop_proccode, out _);
                    }
            }

            //-- fields
            var field_values = new List<string>();
            if (node.TryGetValue(SB3PROP_FIELDS, out JToken prop_fields))
            {
                foreach (var field in prop_fields.Value<JObject>())
                {
                    string name = field.Key;
                    JArray ja = field.Value as JArray;
                    //if (ja == null || ja.Count<2) continue;
                    string fieldvalue = ja[0].Value<JValue>().ToString();
                    if (name == SB3_VARIABLE || name == SB3_LIST) fieldvalue = "[" + fieldvalue + "]";
                    field_values.Add(fieldvalue);
                }
            }

            //-- inputs
            //int fieldvalue_index = 0;
            var input_values = new Dictionary<string, InputValue>();
            if (node.TryGetValue("inputs", out JToken prop_inputs))
            {
                //-- reorder SUBSTACK/SUBSTACK2 as it can arbitraty based on how user placed it
                //TODO: temp hack, to be refactored with a better idea
                var inputs = prop_inputs.Value<JObject>()
                    .Cast<KeyValuePair<string, JToken>>()
                    .OrderBy(item => item.Key);

                foreach (KeyValuePair<string, JToken> input in inputs)
                {
                    JArray values = input.Value as JArray;
                    if (values == null || values.Count < 2) continue;
                    int mainvaluetype = int.Parse(values[0].ToString());

                    //-- check out substacks
                    string input_name = input.Key;
                    if (input_name.StartsWith(SB3_SUBSTACK)) // SB3_SUBSTACK, SB3_SUBSTACK2 type==2
                    {
                        string noderefid = values[1].Value<string>();
                        JProperty refnode = blockroot.Property(noderefid);

                        if (input_name != SB3_SUBSTACK)
                        {
                            if (input_name == SB3_SUBSTACK2 && opcode == OPCODE_CONTROLIFELSE) retval.Add("else:");
                            else retval.Add(input_name);
                        }
                        isownerblock = true;
                        IEnumerable<string> substack = GetStackContents(refnode, false);
                        foreach (string line2 in substack)
                            retval.Add(INDENT_PREFIX + line2);

                        continue;
                    }

                    // 1 if the input is a shadow, 2 if there is no shadow, and 3 if there is a shadow but it is obscured by the input
                    // The second is either the ID of the input or an array representing it as described in the table below.
                    // If there is an obscured shadow, the third element is its ID or an array representing it
                    if (mainvaluetype == (int)blocktype.shadow)
                    {
                        if (values[1] is JArray)
                        {
                            //-- direct value
                            var valuesvalues = values[1] as JArray;
                            int valuetype = int.Parse(valuesvalues[0].ToString());
                            //if (valuetype == (int)blocktype.number || valuetype == (int)blocktype.string_bt)
                            {
                                // actual value
                                string value = valuesvalues[1].ToString();
                                input_values[input.Key] = new InputValue(value, valuetype.ToString());
                            }
                        }
                        else if (values[1] is JValue && values[1].Value<string>().Length == 20)
                        {
                            //-- shadow reference
                            string noderefid = values[1].Value<string>();
                            JObject refnode = blockroot[noderefid] as JObject;

                            //-- inputs | custom_block ==> procedures_prototype
                            if (opcode == OPCODE_PROCDEF && input.Key == SB3_CUSTOMBLOCK)
                            {
                                if (refnode.TryGetValue(SB3PROP_MUTATION, out JToken prop_mutation))
                                {
                                    JObject obj_mutation = prop_mutation.Value<JObject>();
                                    if (obj_mutation.TryGetValue(SB3PROP_PROCCODE, out JToken prop_proccode))
                                    {
                                        //block_name += _ExtractFunctionName(prop_proccode, out string[] argument_types);
                                        block_name = "def " + _ExtractFunctionName(prop_proccode, out string[] argument_types);

                                        obj_mutation.TryGetValue(SB3PROP_ARGNAMES, out JToken prop_argumentnames);
                                        var ss1 = prop_argumentnames.Value<JToken>().ToString();
                                        var ja1 = JArray.Parse(ss1);
                                        obj_mutation.TryGetValue(SB3PROP_ARGDEFS, out JToken prop_argumentdefaults);
                                        var ss2 = prop_argumentdefaults.Value<JToken>().ToString();
                                        var ja2 = JArray.Parse(ss2);


                                        for (int i = 0; i < ja1.Count; i++)
                                            input_values[ja1[i].ToString()] = new InputValue(ja2[i].ToString(), argument_types[i]);

                                        //TODO: argumentdefaults

                                        //prop_argumentnames
                                        //input_values[input.Key] = $"[{varname}]";
                                    }
                                }
                            }
                            else
                            {
                                //-- shadow reference for a dropdown field
                                refnode.TryGetValue(SB3PROP_FIELDS, out JToken reference_prop_fields2);
                                refnode.TryGetValue(SB3PROP_OPCODE, out JToken reference_prop_opcode);
                                string refopcode = reference_prop_opcode.Value<string>();
                                var reference_field0 = reference_prop_fields2.FirstOrDefault() as JProperty;
                                string refname = reference_field0.Name;
                                string reference_field00_val = reference_field0.Value.FirstOrDefault().Value<string>();
                                //var x = 1;
                                if (TRANSLATORS.ContainsKey(refopcode))
                                    if (TRANSLATORS[refopcode]?.TryGetValue(reference_field00_val, out string ref_translated_value) == true)
                                        reference_field00_val = ref_translated_value;
#if PRINT_UNKNOWN_TRANSLATIONS
                                    else
                                        Console.WriteLine($">> {refopcode} {reference_field00_val}"); //!!
                                else
                                    Console.WriteLine($">> {refopcode}"); //!!
#endif

                                input_values[input.Key] = new InputValue(reference_field00_val, refname);
                            }
                        }
                    }
                    else if (mainvaluetype == (int)blocktype.inputwithshadow || mainvaluetype == (int)blocktype.noshadow)
                    {
                        //-- embedded value
                        var jt_reference = values[1].Value<JToken>();
                        if (jt_reference.Type == JTokenType.Array)
                        {
                            //-- variable 
                            JArray ja_reference = jt_reference as JArray;
                            //ja_reference[0] == 12
                            //ja_reference[1] // name
                            string varname = ja_reference[1].ToString();
                            string varrefid = ja_reference[2].ToString(); //-- reference for variable

                            input_values[input.Key] = new InputValue($"[{varname}]", NLFTYPE_VARIABLE);
                        }
                        else if (jt_reference.Type == JTokenType.Null)
                        {
                            //-- rarely happens, e.g. call procedure parameter is empty
                            input_values[input.Key] = new InputValue("null", "NULL");
                        }
                        else
                        {
                            //-- embedded reference
                            string noderefid = jt_reference.ToString();
                            JObject refnode = blockroot[noderefid] as JObject;

                            //????
                            var retval1 = GetNodeContents(refnode, out _, first_node, true); //!!
                            input_values[input.Key] = new InputValue(string.Join(",", retval1.ToArray()), NLFTYPE_EMBEDDED);
                        }
                    }

                    ////-- add postfix
                    //if (field_values.Count > fieldvalue_index)
                    //{
                    //    input_values[input.Key] += " " + field_values[fieldvalue_index];
                    //    fieldvalue_index++;
                    //}
                }
            }

            //-- display keyname when
            //--    either proc_definition OR
            //--    reserved value (proc_calls have autogenerated ides, that we skip) AND  
            //--        more than one value - with one param we skip name
            //--        more values hava same type - if each value type if different, we skip
            bool display_key_names = (opcode == OPCODE_PROCDEF) ||
                (opcode != OPCODE_PROCCALL &&
                //input_values.Count()>1 &&
                input_values.GroupBy(item => item.Value.ValueType).Any(itemgroup =>
                {
                    //-- if only one of a kind, do not display keyname
                    if (itemgroup.Count() == 1) return false;
                    //-- if OPERAND1,OPERAND2, ..., do not display keyname
                    string firstkey = itemgroup.First().Key;
                    if (firstkey.Last() == '1' &&
                        itemgroup.All(gitem => gitem.Key.StartsWith(firstkey.Substring(0, firstkey.Length - 1)))) return false;

                    return true;
                }));
            //(opcode == "procedures_definition") || (kvp.Key.ToUpper() == kvp.Key && input_values.Count>1) ?

            //-- replace node line
            {
                var paramarr =
                    field_values.Concat(
                    input_values.Select(kvp => (display_key_names ? kvp.Key + "=" : null)
                        + kvp.Value.Value)
                    );
                string sparams = string.Join(", ", paramarr.ToArray());
                string printvalue = $"{block_name}({sparams})" + (isownerblock ? ":" : null);
                retval[idx_blockname_placeholder] = printvalue; //-- insert to the front
            }

            return retval;
        }

        /// <summary>
        /// Extract MyBlock function name
        /// </summary>
        /// <param name="prop_proccode"></param>
        /// <param name="argument_types"></param>
        /// <returns></returns>
        static private string _ExtractFunctionName(JToken prop_proccode, out string[] argument_types)
        {
            string proccode = prop_proccode.ToString();
            int idx_of_first_argument = proccode.IndexOf(" %");
            string proccode_functionname = (idx_of_first_argument < 0 ? proccode : proccode.Substring(0, idx_of_first_argument));
            //argumentids, argumentnames, argumentdefaults
            //!!
            //-- find arguments
            argument_types = proccode.Split(' ').Where(item => item == "%s" || item == "%b").ToArray();

            return proccode_functionname;
        }
    }
}