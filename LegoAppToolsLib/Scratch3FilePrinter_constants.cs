using System.Collections.Generic;

namespace LegoAppToolsLib
{
    internal static partial class Scratch3FilePrinter
    {
        enum blocktype
        {
            shadow = 1,
            noshadow = 2,
            inputwithshadow = 3,
            number = 4,
            number_posiitve = 5,
            positive_integer = 6,
            integer = 7,
            angle = 8,
            color = 9,
            string_bt = 10,
            broadcast = 11,
            variable = 12,
            list = 13
        }

        private const string SB3PROP_TOPLEVEL = "topLevel";
        private const string OPCODE_PROCDEF = "procedures_definition";
        private const string OPCODE_PROCCALL = "procedures_call";
        private const string OPCODE_CONTROLIFELSE = "control_if_else";
        private const string SB3PROP_NEXT = "next";
        private const string SB3PROP_PARENT = "parent";
        private const string SB3PROP_OPCODE = "opcode";
        private const string SB3PROP_MUTATION = "mutation";
        private const string SB3PROP_PROCCODE = "proccode";
        private const string SB3PROP_FIELDS = "fields";
        private const string SB3PROP_ARGNAMES = "argumentnames";
        private const string SB3PROP_ARGDEFS = "argumentdefaults";
        private const string SB3PROP_COMMENT = "comment";
        private const string SB3_VARIABLE = "VARIABLE";
        private const string SB3_LIST = "LIST";
        private const string SB3_SUBSTACK = "SUBSTACK";
        private const string SB3_SUBSTACK2 = "SUBSTACK2";
        private const string SB3_CUSTOMBLOCK = "custom_block";
        private const string NLFTYPE_EMBEDDED = "EMBEDDED";
        private const string NLFTYPE_VARIABLE = "VARIABLE";
        private const string INDENT_PREFIX = "  ";
        private const string COMMENT_PREFIX = "# ";
        private const int X_OFFSET_WORDBLOCKS = 350;
        private const int Y_OFFSET_ICONBLOCKS = 60;

        //TODO: LMS translation, EV3 translation //TODO: Wedo2 app
        static readonly Dictionary<string, Dictionary<string, string>> TRANSLATORS = new Dictionary<string, Dictionary<string, string>>()
        {
            //["flippermove_movement-port-selector"] = null,      //-- values are ok e.g. AB
            //["flippersensors_color-sensor-selector"] = null,    //-- values are ok e.g. A
            //["flippermotor_multiple-port-selector"] = null,     //-- values are ok e.g. A
            //["flippermoremotor_multiple-port-selector"] = null, //-- values are ok e.g. A
            //["flipperdisplay_custom-matrix-port"] = null,       //-- values are ok e.g. M1
            //["flipperdisplay_custom-icon-direction"] = null,    //-- values are ok e.g. clockwise,anticlockwise,
            //["flipperdisplay_custom-matrix"] = null,            //-- values are ok e.g. 9909999099000009000909990
            //["flippermotor_custom-angle"] = null,               //-- values are ok e.g. 0
            //["flippermove_custom-icon-direction"] = null,       //-- values are ok e.g. shortest,clockwise,anticlockwise,
            //["flippermotor_custom-icon-direction"] = null,      //-- values are ok e.g. shortest,clockwise,anticlockwise,
            //["flipperpoweredup_wedo-motion-sensor-selector"] = null,    //-- values are ok e.g. A
            //["flipperpoweredup_wedo-tilt-sensor-selector"] = null,      //-- values are ok e.g. A
            //["flipperpoweredup_boost-vision-sensor-selector"] = null,   //-- values are ok e.g. A
            //["flippermoresensors_color-sensor-selector"] = null,        //-- values are ok e.g. A
            //["flippermotor_single-motor-selector"] = null,      //-- values are ok
            //["flippersensors_force-sensor-selector"] = null,      //-- values are ok
            //["flippermotor_single-motor-selector"] = null,      //-- values are ok
            //["flippersensors_force-sensor-selector"] = null,      //-- values are ok
            //["flippersound_sound-selector"] = null,      //-- values are ok
            //["weather_menu_forecastTo"] = null,                 //-- values are ok e.g. 2,3,4,..9

            ["flipperevents_color-selector"] =
                new Dictionary<string, string>()
                {
                    ["-1"] = "NoColor",
                    ["0"] = "Black",
                    ["1"] = "Violet",
                    //["2"] = "",
                    ["3"] = "Blue",
                    ["4"] = "Turquoise", //LightBlue
                    ["5"] = "Green",
                    //["6"] = "",
                    ["7"] = "Yellow",
                    //["8"] = "orange",
                    ["9"] = "Red",
                    ["10"] = "White",
                },
            ["flippersensors_color-selector"] =
                new Dictionary<string, string>()
                {
                    ["-1"] = "NoColor",
                    ["0"] = "Black",
                    ["1"] = "Violet",
                    //["2"] = "",
                    ["3"] = "Blue",
                    ["4"] = "Turquoise", //LightBlue
                    ["5"] = "Green",
                    //["6"] = "",
                    ["7"] = "Yellow",
                    //["8"] = "orange",
                    ["9"] = "Red",
                    ["10"] = "White",
                },
            ["horizontalevents_horizontal-color-sensor"] =
                new Dictionary<string, string>()
                {
                    ["-1"] = "NoColor",
                    ["0"] = "Black",
                    ["1"] = "Violet",
                    //["2"] = "",
                    ["3"] = "Blue",
                    ["4"] = "Turquoise", //LightBlue
                    ["5"] = "Green",
                    //["6"] = "",
                    ["7"] = "Yellow",
                    //["8"] = "orange",
                    ["9"] = "Red",
                    ["10"] = "White",
                },


            ["flipperevents_custom-tilted"] =
                new Dictionary<string, string>()
                {
                    ["1"] = "Front",
                    ["2"] = "Back",
                    ["3"] = "Top",
                    ["4"] = "Bottom",
                    ["5"] = "Any",
                    ["6"] = "None",
                },
            ["flippersensors_custom-tilted"] =
                new Dictionary<string, string>()
                {
                    ["1"] = "Front",
                    ["2"] = "Back",
                    ["3"] = "Top",
                    ["4"] = "Bottom",
                    ["5"] = "Any",
                    ["6"] = "None",
                },
            ["horizontalevents_HorizontalTilted"] =
                new Dictionary<string, string>()
                {
                    ["1"] = "Front",
                    ["2"] = "Back",
                    ["3"] = "Top",
                    ["4"] = "Bottom",
                    ["5"] = "Any",
                    ["6"] = "None",
                },

            ["flipperdisplay_color-selector-vertical"] =
                new Dictionary<string, string>()
                {
                    ["0"] = "Off",
                    ["1"] = "Magenta",
                    ["2"] = "Violet",
                    ["3"] = "Blue",
                    ["4"] = "Turquoise",
                    ["5"] = "Mint",
                    ["6"] = "Green",
                    ["7"] = "Yellow",
                    ["8"] = "Orange",
                    ["9"] = "Red",
                    ["10"] = "White",
                },

            ["flipperdisplay_menu_orientation"] =
                new Dictionary<string, string>()
                {
                    ["1"] = "Upright",
                    ["2"] = "Left",
                    ["3"] = "Right",
                    ["4"] = "UpsideDown",
                },


            ["flippermoremotor_menu_acceleration"] =
                new Dictionary<string, string>()
                {
                    ["-1 -1"] = "Default",
                    ["100 100"] = "Fast",
                    ["350 350"] = "Balanced",
                    ["800 800"] = "Smooth",
                    ["1200 1200"] = "Slow",
                    ["2000 2000"] = "VerySlow",
                },
            ["flippermoremove_menu_acceleration"] =
                new Dictionary<string, string>()
                {
                    ["-1 -1"] = "Default",
                    ["100 100"] = "Fast",
                    ["350 350"] = "Balanced",
                    ["800 800"] = "Smooth",
                    ["1200 1200"] = "Slow",
                    ["2000 2000"] = "VerySlow",
                },

            ["flippermusic_menu_DRUM"] =
                new Dictionary<string, string>()
                {
                    ["X"] = "X",
                },
            ["flippermusic_menu_INSTRUMENT"] =
                new Dictionary<string, string>()
                {
                    ["X"] = "X",
                },

            ["flipperpoweredup_boost-vision-color-selector"] =
                new Dictionary<string, string>()
                {
                    ["0"] = "Black",
                    ["3"] = "Blue",
                    ["5"] = "Green",
                    ["7"] = "Yellow",
                    ["9"] = "Red",
                    ["10"] = "White",
                    ["-1"] = "NoColor",
                },
            ["flipperpoweredup_boost-vision-color-selector-vertical"] =
                new Dictionary<string, string>()
                {
                    ["0"] = "Black",
                    ["3"] = "Blue",
                    ["5"] = "Green",
                    //["7"] = "Yellow",
                    ["9"] = "Red",
                    ["10"] = "White",
                    //["-1"] = "NoColor",
                },

            ["bargraphmonitor_custom-color"] =
                new Dictionary<string, string>()
                {
                    ["1"] = "Violet",
                    ["3"] = "Blue",
                    ["4"] = "LightBlue",
                    ["5"] = "Green",
                    ["7"] = "Yellow",
                    ["9"] = "Red",
                },

            ["linegraphmonitor_custom-color"] =
                new Dictionary<string, string>()
                {
                    ["1"] = "Violet",
                    ["3"] = "Blue",
                    ["4"] = "LightBlue",
                    ["5"] = "Green",
                    ["7"] = "Yellow",
                    ["9"] = "Red",
                },
        };
    }
}