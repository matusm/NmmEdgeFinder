using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bev.IO.NmmReader;
using Bev.IO.NmmReader.scan_mode;
using Bev.UI;

namespace NmmEdgeFinder
{
    class Program
    {
        static void Main(string[] args)
        {
 
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            // parse command line arguments
            var options = new Options();
            if (!CommandLine.Parser.Default.ParseArgumentsStrict(args, options))
                Console.WriteLine("*** ParseArgumentsStrict returned false");
            
            // consume the verbosity option
            if (options.BeQuiet == true)
                ConsoleUI.BeSilent();
            else
                ConsoleUI.BeVerbatim();
            ConsoleUI.Welcome();
           
            // forward only and backward only does not make sense
            if(options.FwOnly&&options.BwOnly)
            {
                options.FwOnly = false;
                options.BwOnly = false;
            }

            // get the filename(s)
            string[] fileNames = options.ListOfFileNames.ToArray();
            if (fileNames.Length == 0)
                ConsoleUI.ErrorExit("!Missing input file name", 1);
            
            // read all relevant scan data
            ConsoleUI.StartOperation("Reading NMM scan files");
            NmmFileName nmmFileName = new NmmFileName(fileNames[0]);
            nmmFileName.SetScanIndex(options.ScanIndex);
            NmmScanData nmmScanData = new NmmScanData(nmmFileName);
            ConsoleUI.Done();

            // check if data present
            if (nmmScanData.MetaData.ScanStatus == ScanDirectionStatus.Unknown)
                ConsoleUI.ErrorExit("!Unknown scan type", 5);
            if (nmmScanData.MetaData.ScanStatus == ScanDirectionStatus.NoData)
                ConsoleUI.ErrorExit("!No scan data present", 6);

            // Check if requested channels are present in raw data
            if (!nmmScanData.ColumnPresent(options.ChannelSymbol))
                ConsoleUI.ErrorExit($"!Requested channel {options.ChannelSymbol} not in data files", 2);
            if (!nmmScanData.ColumnPresent("LX"))
                ConsoleUI.ErrorExit($"!Channel {"LX"} not in data files", 3);
            if (!nmmScanData.ColumnPresent("LY"))
                ConsoleUI.ErrorExit($"!Channel {"LY"} not in data files", 4);

            // some screen output
            ConsoleUI.WriteLine($"SpuriousDataLines: {nmmScanData.MetaData.SpuriousDataLines}");
            ConsoleUI.WriteLine($"NumberOfGlitchedDataPoints: {nmmScanData.MetaData.NumberOfGlitchedDataPoints}");
            ConsoleUI.WriteLine($"{nmmScanData.MetaData.NumberOfDataPoints} data lines with {nmmScanData.MetaData.NumberOfColumnsInFile} channels, organized in {nmmScanData.MetaData.NumberOfProfiles} profiles");
            ConsoleUI.WriteLine($"z-axis channel: {options.ChannelSymbol}");
            ConsoleUI.WriteLine($"Threshold: {options.Threshold}");

            List<EdgePoint> edgePoints = new List<EdgePoint>();
            Classifier classifier;
            IntensityEvaluator eval;
            double[] luminanceField;
            double[] luminanceFieldFw;
            double[] luminanceFieldBw;
            double[] laserX;
            double[] laserY;
            int[] segmentedField;

            // evaluate the intensities for ALL profiles == the whole scan field
            ConsoleUI.StartOperation("Classifying intensity data");
            luminanceFieldFw = nmmScanData.ExtractProfile(options.ChannelSymbol, 0, TopographyProcessType.ForwardOnly);
            luminanceFieldBw = nmmScanData.ExtractProfile(options.ChannelSymbol, 0, TopographyProcessType.BackwardOnly);
            if (BwScanPresent())
            {
                luminanceField = luminanceFieldFw.Concat(luminanceFieldBw).ToArray();
            }
            else
            {
                luminanceField = luminanceFieldFw;
            }
            eval = new IntensityEvaluator(luminanceField);
            ConsoleUI.Done();
            ConsoleUI.WriteLine($"Intensity range from {eval.MinIntensity} to {eval.MaxIntensity}");
            ConsoleUI.WriteLine($"Estimated bounds from {eval.LowerBound} to {eval.UpperBound}");
            double relativeSpan = (double)(eval.UpperBound - eval.LowerBound) / (double)(eval.MaxIntensity - eval.MinIntensity) * 100.0;
            ConsoleUI.WriteLine($"({relativeSpan:F1} % of full range)");

            // find edges in the forward scan
            ConsoleUI.StartOperation("Searching edges");
            if (ProcessFwScan())
            {
                classifier = new Classifier(luminanceFieldFw);
                segmentedField = classifier.GetSegmentedProfile(options.Threshold, eval.LowerBound, eval.UpperBound);
                laserX = nmmScanData.ExtractProfile("LX", 0, TopographyProcessType.ForwardOnly);
                laserY = nmmScanData.ExtractProfile("LY", 0, TopographyProcessType.ForwardOnly);
                for (int i = 1; i < segmentedField.Length; i++)
                    if (segmentedField[i - 1] + segmentedField[i] == 1)
                        edgePoints.Add(new EdgePoint(laserX[i], laserY[i], i, ScanDirection.Forward));
            }
            // find edges in the backward scan (if present)
            if (ProcessBwScan())
            {
                classifier = new Classifier(luminanceFieldBw);
                segmentedField = classifier.GetSegmentedProfile(options.Threshold, eval.LowerBound, eval.UpperBound);
                laserX = nmmScanData.ExtractProfile("LX", 0, TopographyProcessType.BackwardOnly);
                laserY = nmmScanData.ExtractProfile("LY", 0, TopographyProcessType.BackwardOnly);
                for (int i = 1; i < segmentedField.Length; i++)
                    if (segmentedField[i - 1] + segmentedField[i] == 1)
                        edgePoints.Add(new EdgePoint(laserX[i], laserY[i], i, ScanDirection.Backward));
            }
            ConsoleUI.Done();

            ExportEdgeAsCsv();

            bool ProcessFwScan()
            {
                return !options.BwOnly;
            }

            bool ProcessBwScan()
            {
                return (BwScanPresent() && !options.FwOnly);
            }

            bool BwScanPresent()
            {
                return (nmmScanData.MetaData.ScanStatus == ScanDirectionStatus.ForwardAndBackward || nmmScanData.MetaData.ScanStatus == ScanDirectionStatus.ForwardAndBackwardJustified);
            }

            // very dirty implementation of the CSV file writer for the edge points
            void ExportEdgeAsCsv()
            {
                string csvFileName = nmmFileName.GetFreeFileNameWithIndex("csv");
                try
                {
                    StreamWriter hCsvFile = File.CreateText(csvFileName);
                    ConsoleUI.WritingFile(csvFileName);
                    hCsvFile.WriteLine($"# {ConsoleUI.Title} v{ConsoleUI.Version}");
                    hCsvFile.WriteLine($"# Threshold          = {options.Threshold:F2}");
                    hCsvFile.WriteLine($"# Comment            = {options.UserComment}");
                    hCsvFile.WriteLine($"# SampleTemperature  = {nmmScanData.MetaData.SampleTemperature:F3}");
                    hCsvFile.WriteLine($"# Forward scan used  = {ProcessFwScan()}");
                    hCsvFile.WriteLine($"# Backward scan used = {ProcessBwScan()}");
                    hCsvFile.WriteLine($"# Number of points   = {edgePoints.Count}");
                    hCsvFile.WriteLine("x_global , y_global"); 
                    hCsvFile.WriteLine("m , m");
                    foreach (var ep in edgePoints)
                    {
                        hCsvFile.WriteLine($"{ep.X:F10} , {ep.Y:F10}");
                    }
                    hCsvFile.Close();
                    ConsoleUI.Done();
                }
                catch (Exception)
                {
                    // file problem, just ignore
                }
            }

        }
    }
}
