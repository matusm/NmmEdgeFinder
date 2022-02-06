using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace NmmEdgeFinder
{
    class Options
    {
        [Option('t', "threshold", DefaultValue = 0.5, HelpText = "Threshold for segmentation.")]
        public double Threshold { get; set; }

        [Option('c', "channel", DefaultValue = "AX", HelpText = "Channel to analyze.")]
        public string ChannelSymbol { get; set; }

        [Option('s', "scan", DefaultValue = 0, HelpText = "Scan index for multi-scan files.")]
        public int ScanIndex { get; set; }

        [Option('f', "forwardOnly", HelpText = "Use forward scan data only.")]
        public bool FwOnly { get; set; }

        [Option('b', "backwardOnly", HelpText = "Use backward scan data only (if present).")]
        public bool BwOnly { get; set; }

        [Option('q', "quiet", HelpText = "Quiet mode. No screen output (except for errors).")]
        public bool BeQuiet { get; set; }

        [Option("comment", DefaultValue = "---", HelpText = "User supplied comment string.")]
        public string UserComment { get; set; }

        [ValueList(typeof(List<string>), MaximumElements = 2)]
        public IList<string> ListOfFileNames { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            string AppName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            string AppVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            HelpText help = new HelpText
            {
                Heading = new HeadingInfo(AppName, "version " + AppVer),
                Copyright = new CopyrightInfo("Michael Matus", 2022),
                AdditionalNewLineAfterOption = false,
                AddDashesToOption = true
            };
            string sPre = "Program to find edges in the intensity channel of scanning files produced by the SIOS NMM-1. " +
                "The quadruple of files (dat ind dsc pos) are analyzed to obtain the required parameters. " +
                "x and y coordinates of detected edges are output to a csv file.";
            help.AddPreOptionsLine(sPre);
            help.AddPreOptionsLine("");
            help.AddPreOptionsLine("Usage: " + AppName + " filename1 [filename2] [options]");
            help.AddPostOptionsLine("");
            help.AddOptions(this);
            return help;
        }


    }
}
