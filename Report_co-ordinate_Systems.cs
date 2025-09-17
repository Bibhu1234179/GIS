using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QC_Tools.Tools
{
    class Report_Coordinate_System
    {
        public static DateTime startTime;
        public static DateTime endTime;
        public static double UNTolerance;
        public static async Task Process(System.Windows.Controls.Label lblpro)
        {
            startTime = DateTime.Now;
            string uniqueid = Dockpane1View.uniquefield;
            DateTime currentTime = DateTime.Now;
            string timestamp = currentTime.ToString("yyyy-MM-dd_HH-mm-ss");
            string rptName_1 = $"{"Report_Co_ordinate_Systems"}_{timestamp}.csv";
            StreamWriter stw_1 = new StreamWriter(System.IO.Path.Combine(Dockpane1View.reportpath, rptName_1));


            await QueuedTask.Run(() =>
            {
                stw_1.WriteLine("Feature Class Name, ProjectedCS, GeographicCS, VerticalCS");
                ReportCS(stw_1, lblpro);
                //lblpro.Dispatcher.Invoke(() => lblpro.Content = "Processing complete.");
            });
            endTime = DateTime.Now;
            cls_required_methods.writelogtimetocsv("log_reportcoordinatesystems", "Report co-ordinate System of all feature classes", endTime, startTime);
        }
        public static void ReportCS(StreamWriter sw, System.Windows.Controls.Label lblpro)
        {
            try
            {
                using (Geodatabase sourcegdb = new Geodatabase(
                    new FileGeodatabaseConnectionPath(new Uri(Dockpane1View.gdbpath))))
                {
                    var fcDefs = sourcegdb.GetDefinitions<FeatureClassDefinition>();


                    foreach (var fcDef in fcDefs)
                    {
                        try
                        {
                            using var fc = sourcegdb.OpenDataset<FeatureClass>(fcDef.GetName());
                            var def = fc.GetDefinition();
                            var sr = def.GetSpatialReference();

                            string pcs = sr.IsProjected ? sr.Name : "None";

                            // Geographic CS (base GCS if projected, or itself if geographic)
                            string gcs = sr.Gcs != null ? sr.Gcs.Name : "None";

                            // Vertical CS
                            string vcs = "None";
                            if (sr.VcsWkid > 0)
                            {
                                var vertList = GeometryEngine.Instance
                                    .GetPredefinedCoordinateSystemList(CoordinateSystemFilter.VerticalCoordinateSystem);

                                var match = vertList.FirstOrDefault(v => v.Wkid == sr.VcsWkid);
                                vcs = match != null ? match.Name : $"WKID {sr.VcsWkid}";
                            }
                            string name = fc.GetName();
                            lblpro.Dispatcher.Invoke(() => lblpro.Content = $"Processing Feature Class: {name}");


                            sw.WriteLine($"{fc.GetName()},{pcs},{gcs},{vcs}");
                        }
                        catch (Exception ex)
                        {
                            sw.WriteLine($"{fcDef.GetName()},Error,Error,Error");
                            System.Diagnostics.Debug.WriteLine(
                                $"Error processing {fcDef.GetName()}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle errors that occur before/after loop
                sw.WriteLine($"GeneralError,Error,Error,Error");
                System.Diagnostics.Debug.WriteLine($"Unexpected error: {ex.Message}");
            }
            finally
            {
                // Ensure flush + reset label
                sw.Close();
                sw.Dispose();

                lblpro.Dispatcher.Invoke(() => lblpro.Content = "Processing complete.");
            }


        }
    }
}
