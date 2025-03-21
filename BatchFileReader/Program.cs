using System;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Collections.Generic;

//open cmd at the location of the file
//cmd: dotnet run

namespace BatchFileReader
{
   class Program
   {
      static void Main(string[] args)
      {
         // Check which mode we're running in
         string mode = args.Length > 0 ? args[0] : "GRun";

         if (mode == "GRun")//Generate Run
         {
            ProcessNewFolder();
         }
         else if (mode == "SpeedRun")
         {
            ProcessOldFolder();
         }
      }

      // ================== Speed Run ==================
      static void ProcessOldFolder()
      {

         // Path to the folder containing files to be processed
         string folderPath = @"C:\Users\78500\AppData\LocalLow\DefaultCompany\Dummy2D\LogsToRead"; // Change this to your folder path
         // Export the data to an Excel file
         string excelFilePath = @"C:\Users\78500\AppData\LocalLow\DefaultCompany\Dummy2D\ExcelOutput.xlsx"; // Change this to your desired output path
         string[] files = Directory.GetFiles(folderPath);

         // Data structure to hold processed data
         List<FileData> processedData = new List<FileData>();

         // Loop through each file in the folder
         foreach (var file in files)
         {
            // Example: Read each file line by line (adjust this logic based on your needs)
            string[] lines = File.ReadAllLines(file);
            List<string> summaryLines = ExtractSummaryLines(lines);

            if (summaryLines.Count > 0)
            {

               // Process file content (example: count lines, or extract data)
               processedData.Add(new FileData
               {
                  FileName = Path.GetFileName(file),
                  SummaryLines = summaryLines
               });
            }

         }
         ExportDataToExcelSpeedRun(processedData, excelFilePath);
         Console.WriteLine("Batch file processing completed and data exported to " + excelFilePath);
      }


      // Function to extract lines between "===Summary===" markers
      static List<string> ExtractSummaryLines(string[] lines)
      {
         List<string> summaryLines = new List<string>();
         bool insideSummary = false;

         foreach (var line in lines)
         {
            if (line.Trim() == "===Summary===")
            {
               insideSummary = !insideSummary; // Toggle the state
               continue;
            }

            if (insideSummary)
            {
               summaryLines.Add(line.Trim());
            }
         }

         return summaryLines;
      }



      // Function to export data to Excel
      static void ExportDataToExcelSpeedRun(List<FileData> data, string excelFilePath)
      {
         // Initialize the ExcelPackage (EPPlus)
         using (ExcelPackage package = new ExcelPackage())
         {
            // Create a worksheet
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Processed Data");

            // Set the headers
            worksheet.Cells[1, 1].Value = "File Name";
            /*
            writer.WriteLine($"{dimensions}x{dimensions}"); //dimensions
            writer.WriteLine($"{timeDelayPerStep}"); //speed
            writer.WriteLine($"{player.totalSteps}"); //steps
            writer.WriteLine($"{playerEndTime - playerStartTime:F2}"); //total time
            writer.WriteLine($"{averageTime:F2}"); //wfc average time
            writer.WriteLine($"{gridCount}");//grid count
            writer.WriteLine($"{cellCount}");//cell count
            writer.WriteLine($"{tileDropdown.value}");//tile type
            */
            worksheet.Cells[1, 2].Value = "NxN";
            worksheet.Cells[1, 3].Value = "Speed";
            worksheet.Cells[1, 4].Value = "Steps";
            worksheet.Cells[1, 5].Value = "Total Time";
            worksheet.Cells[1, 6].Value = "WFC Average Time";
            worksheet.Cells[1, 7].Value = "Grid Count";
            worksheet.Cells[1, 8].Value = "Cell Count";
            worksheet.Cells[1, 9].Value = "Tile Type";


            // Make headers bold
            worksheet.Cells[1, 1, 1, 3].Style.Font.Bold = true;

            // Populate the worksheet with data
            int row = 2;
            foreach (var fileData in data)
            {
               worksheet.Cells[row, 1].Value = fileData.FileName;
               // Place each summary line in its own column, starting from column 2
               for (int i = 0; i < fileData.SummaryLines.Count; i++)
               {
                  worksheet.Cells[row, i + 2].Value = fileData.SummaryLines[i];
               }
               row++;
            }

            // Auto fit columns for better readability
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            // Save the file
            FileInfo excelFile = new FileInfo(excelFilePath);
            package.SaveAs(excelFile);
         }
      }

      // Class to store file data
      class FileData
      {
         public string FileName { get; set; }
         public List<string> SummaryLines { get; set; }
      }

      // ================== End of Speed Run ==================



      // ================== Generate Run ==================

      static void ProcessNewFolder()
      {
         string folderPath = @"C:\Users\78500\AppData\LocalLow\DefaultCompany\Dummy2D\GenSpeedTest";
         string excelFilePath = @"C:\Users\78500\AppData\LocalLow\DefaultCompany\Dummy2D\ExcelOutputGSpd.xlsx";
         string[] files = Directory.GetFiles(folderPath);
         // List to hold extracted data
         List<TestGSData> processedData = new List<TestGSData>();

         // Loop through each file
         foreach (var file in files)
         {
            string[] lines = File.ReadAllLines(file);
            string fileName = Path.GetFileNameWithoutExtension(file);

            TestGSData testData = ExtractSpeedTestData(fileName, lines);

            if (testData != null)
            {
               processedData.Add(testData);
            }
         }

         // Export data to Excel
         ExportDataToExcelGSpd(processedData, excelFilePath);

         Console.WriteLine("Processing completed, and data exported to Excel.");


      }

      // Extracts NxN, Grid #, and Time Total from the file name and lines in the file
      static TestGSData ExtractSpeedTestData(string fileName, string[] lines)
      {
         int nxn = 0; // Store the numeric part of NxN
         int gridNumber = 0;
         double totalTime = 0;

         // Example filename: "SpeedTest_3x3_10_Grids_09_20_20_49_37"
         string[] fileNameParts = fileName.Split('_');

         if (fileNameParts.Length >= 3)
         {
            // Extract only the numeric part before 'x'
            string[] dimensionParts = fileNameParts[1].Split('x');
            nxn = int.Parse(dimensionParts[0]);  // Example: Extract 3 from "3x3"
            gridNumber = int.Parse(fileNameParts[2]); // Example: 10 (grid number)
         }

         // Parse file content for grid size and total time
         foreach (var line in lines)
         {
            if (line.StartsWith("Grid size:"))
            {
               string[] gridSizeParts = line.Split(':')[1].Trim().Split('x');
               nxn = int.Parse(gridSizeParts[0]);  // Example: Extract 3 from "3x3"
            }
            else if (line.StartsWith("Total grids generated:"))
            {
               gridNumber = int.Parse(line.Split(':')[1].Trim());
            }
            else if (line.StartsWith("Total time taken:"))
            {
               totalTime = double.Parse(line.Split(':')[1].Replace("seconds", "").Trim());
            }
         }

         return new TestGSData
         {
            NxN = nxn,
            GridNumber = gridNumber,
            TotalTime = totalTime
         };
      }

      // Function to export data to Excel
      static void ExportDataToExcelGSpd(List<TestGSData> data, string excelFilePath)
      {
         // Initialize the ExcelPackage (EPPlus)
         using (ExcelPackage package = new ExcelPackage())
         {
            // Create a worksheet
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Speed Test Results");

            // Set the headers
            worksheet.Cells[1, 1].Value = "NxN";
            worksheet.Cells[1, 2].Value = "Grid #";
            worksheet.Cells[1, 3].Value = "Time Total";

            // Make headers bold
            worksheet.Cells[1, 1, 1, 3].Style.Font.Bold = true;

            // Populate the worksheet with data
            int row = 2;
            foreach (var testData in data)
            {
               worksheet.Cells[row, 1].Value = testData.NxN;
               worksheet.Cells[row, 2].Value = testData.GridNumber;
               worksheet.Cells[row, 3].Value = testData.TotalTime;
               row++;
            }

            // Auto fit columns for better readability
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            // Save the file
            FileInfo excelFile = new FileInfo(excelFilePath);
            package.SaveAs(excelFile);
         }
      }
      // Class to store speed test data
      class TestGSData
      {
         public int NxN { get; set; } // Changed from string to int
         public int GridNumber { get; set; }
         public double TotalTime { get; set; }
      }

      // ================== End of Generate Run ==================
   }

   // ================== End of Program ==================
}
