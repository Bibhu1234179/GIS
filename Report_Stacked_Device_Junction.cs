using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace QC_Tools.Tools
{
    class Report_Stacked_Device_Junction
    {
        public static DateTime startTime;
        public static DateTime endTime;

        public static async Task Process(System.Windows.Controls.Label lblpro)
        {
            startTime = DateTime.Now;
            string uniqueid = Dockpane1View.uniquefield;
            DateTime currentTime = DateTime.Now;
            string timestamp = currentTime.ToString("yyyy-MM-dd_HH-mm-ss");
            string rptName = $"{"Report_Stacked_Device"}_{timestamp}.csv";
            StreamWriter sw = new StreamWriter(Path.Combine(Dockpane1View.reportpath, rptName));

            await QueuedTask.Run(() =>
            {
                sw.WriteLine("Device_fc,Device_assetgroup,Device_assettype,Device_globalid,Device_UniqueID,Device_Lifecycle,Junction_fc,Junction_assetgroup,Junction_assettype,Junction_globalid,Junction_UniqueID,Junction_Lifecycle,Distance,Remarks");
                Report_StackDeviceJunction(sw, lblpro, uniqueid);
            });
            endTime = DateTime.Now;
            cls_required_methods.writelogtimetocsv("log_reportstackeddevicejunction", "Report Stacked Devices and Junctions", endTime, startTime);
        }

        public static Domain GetDomainFromField(Row row, Field field)
        {
            // Get the table and table definition from the Row
            using (Table table = row.GetTable())
            using (TableDefinition tableDefinition = table.GetDefinition())
            {
                // Get name of subtype field
                string subtypeFieldName = tableDefinition.GetSubtypeField();

                // Get subtype, if any
                Subtype subtype = null;

                if (subtypeFieldName.Length != 0)
                {
                    // Get value of subtype field for this row
                    var varSubtypeCode = row[subtypeFieldName];
                    int subtypeCode = Convert.ToInt32(varSubtypeCode);
                    try
                    {
                        subtype = tableDefinition.GetSubtypes().First(x => x.GetCode() == subtypeCode);
                    }
                    catch (Exception ex)
                    {

                    }
                }
                CodedValueDomain domain = field.GetDomain(subtype) as CodedValueDomain;
                // Return the domain for this field
                if (domain != null)
                {
                    return domain;
                }
                else
                {
                    return null;
                }
            }
        }
        public Domain GetDomainFromField(FeatureClass fcldev, string fieldname, string subtypedesc)
        {
            IReadOnlyList<Subtype> subtypelist_dev = fcldev.GetDefinition().GetSubtypes();
            TableDefinition tableDefinition = fcldev.GetDefinition();
            Subtype subtype = tableDefinition.GetSubtypes().First(x => x.GetName() == subtypedesc);
            Field fldx = null;
            IReadOnlyList<Field> fields = fcldev.GetDefinition().GetFields();
            foreach (Field fld in fields)
            {
                if (fld.Name.ToLower() == "assettype")
                {
                    fldx = fld;
                }
            }
            CodedValueDomain domain = fldx.GetDomain(subtype) as CodedValueDomain;
            return domain;

        }
        public static string GetDomainDesc(Domain domain, int domaincode)
        {
            string description = "";
            try
            {
                if (domain is CodedValueDomain)
                {
                    CodedValueDomain cdomain = domain as CodedValueDomain;

                    Dictionary<object, string> list = new Dictionary<object, string>();
                    SortedList<object, string> codedValuePairs = cdomain.GetCodedValuePairs();
                    foreach (var item in codedValuePairs)
                    {
                        if (item.Key.ToString() == domaincode.ToString())
                        {
                            description = item.Value;
                            return description;
                        }
                    }
                    //description = list[domaincode];
                    //return description;
                }
                else
                {
                    return description;
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message.ToString());
            }
            return description;
        }
        public static string GetDomainNetwork(Table domainnetworktab)
        {
            string str_domain_network = "";
            try
            {
                using (Table newTBL = domainnetworktab)
                {
                    RowCursor cur = newTBL.Search(null, true);
                    if (cur.MoveNext())
                    {
                        Row row = cur.Current;
                        int num_domaincode = Convert.ToInt32(row["domain_network"]);
                        Field network_field = newTBL.GetDefinition().GetFields().FirstOrDefault(f => f.Name.ToLower().Equals("domain_network".ToLower(), StringComparison.OrdinalIgnoreCase));
                        if (network_field != null)
                        {
                            Domain domain = network_field.GetDomain();//GetDomainFromField(row, row.GetFields()[row.FindField("domain_network")]);
                            str_domain_network = GetDomainDesc(domain, num_domaincode);
                        }
                    }

                }
            }
            catch (Exception ex)
            {

            }
            return str_domain_network;
        }
        public static bool FieldExists(FeatureClass featureClass, string fieldName)
        {
            FeatureClassDefinition def = featureClass.GetDefinition();
            return def.GetFields().Any(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
        }
        public static void Report_StackDeviceJunction(StreamWriter sw, System.Windows.Controls.Label lblpro, string uniquefield)
        {
            using (Geodatabase sourcegdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(Dockpane1View.gdbpath))))
            {
                try
                {
                    Dictionary<string, int> dic_stack_cnt = new Dictionary<string, int>();
                    List<long> oiddupList = new List<long>();

                    Table domnettab = sourcegdb.OpenDataset<Table>("A_DomainNetwork");
                    string domainstring = GetDomainNetwork(domnettab);
                    FeatureClass fcldev = sourcegdb.OpenDataset<FeatureClass>(domainstring + "Device");

                    FeatureClass fcljun = sourcegdb.OpenDataset<FeatureClass>(domainstring + "Junction");

                    if (!(FieldExists(fcljun, uniquefield)))
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Provided unique id field is not valid for Junction");
                        return;
                    }

                    if (!(FieldExists(fcldev, uniquefield)))
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Provided unique id field is not valid for device");
                        return;
                    }
                    IReadOnlyList<Subtype> subtypelist_dev = fcldev.GetDefinition().GetSubtypes();
                    Dictionary<int, string> dicsubtype_dev = new Dictionary<int, string>();

                    foreach (Subtype xx in subtypelist_dev)
                    {
                        dicsubtype_dev.Add(xx.GetCode(), xx.GetName());
                    }
                    IReadOnlyList<Subtype> subtypelist_jun = fcljun.GetDefinition().GetSubtypes();
                    Dictionary<int, string> dicsubtype_jun = new Dictionary<int, string>();

                    foreach (Subtype xx in subtypelist_jun)
                    {
                        dicsubtype_jun.Add(xx.GetCode(), xx.GetName());
                    }
                    FeatureClass fclxx = fcldev;
                    {
                        SpatialQueryFilter sf = new SpatialQueryFilter();
                        int cnt = 0;
                        long cntmax = fclxx.GetCount();

                        var featcur = fclxx.Search(null, false);
                        while (featcur.MoveNext())
                        {
                            try
                            {
                                cnt++;
                                lblpro.Content = "Processing stack DeviceJunction..." + cnt + "/" + cntmax;
                                System.Windows.Forms.Application.DoEvents();

                                Feature ft = featcur.Current as Feature;
                                sf.FilterGeometry = GeometryEngine.Instance.Buffer(ft.GetShape(), Dockpane1View.UN_Tolerance);
                                sf.SpatialRelationship = SpatialRelationship.Intersects;
                                var featcur2 = fcljun.Search(sf, false);

                                while (featcur2.MoveNext())
                                {
                                    try
                                    {
                                        Feature ft2 = featcur2.Current as Feature;
                                        var pt1 = ft.GetShape() as MapPoint;
                                        var pt2 = ft2.GetShape() as MapPoint;

                                        if (pt1.Z == pt2.Z)
                                        {
                                            if (GeometryEngine.Instance.Distance(ft.GetShape(), ft2.GetShape()) < Dockpane1View.UN_Tolerance)
                                            {
                                                double distance = 0;
                                                distance = GeometryEngine.Instance.Distance(ft.GetShape(), ft2.GetShape());
                                                distance = Math.Round(distance, 15);

                                                object assegrop1 = ft["assetgroup"];
                                                object assegrop2 = ft2["assetgroup"];
                                                object assettype1 = ft["assettype"];
                                                Domain dom1 = GetDomainFromField(ft, ft.GetFields()[ft.FindField("assettype")]);
                                                string astype1 = GetDomainDesc(dom1, Convert.ToInt32(assettype1));
                                                object assettype2 = ft2["assettype"];
                                                Domain dom2 = GetDomainFromField(ft2, ft2.GetFields()[ft2.FindField("assettype")]);
                                                string astype2 = GetDomainDesc(dom2, Convert.ToInt32(assettype2));

                                                object globalid_dev = ft["globalid"];
                                                object globalid_jun = ft2["globalid"];

                                                string uniqueid_dev = ft["source_oid"] == null || ft["source_oid"] == DBNull.Value ? "Null" : ft["source_oid"].ToString();
                                                string uniqueid_jun = ft2["source_oid"] == null || ft2["source_oid"] == DBNull.Value ? "Null" : ft2["source_oid"].ToString();

                                                int assettype_dev = Convert.ToInt32(ft["assettype"]);
                                                int assetgroup_dev = Convert.ToInt32(ft["assetgroup"]);
                                                string dev_asstypdesc = cls_required_methods.GetAssetTypeDescription(fcldev, assetgroup_dev, assettype_dev, out string dev_assgrpdesc);

                                                int assettype_jun = Convert.ToInt32(ft2["assettype"]);
                                                int assetgroup_jun = Convert.ToInt32(ft2["assetgroup"]);
                                                string jun_asstypdesc = cls_required_methods.GetAssetTypeDescription(fcljun, assetgroup_jun, assettype_jun, out string jun_assgrpdesc);

                                                int pointLifecycleStatus_1 = Convert.ToInt32(ft["lifecyclestatus"]);
                                                string lifecycle_dev = cls_required_methods.GetLifecyclestatus(fcldev, "lifecyclestatus", int.Parse(pointLifecycleStatus_1.ToString()));

                                                int pointLifecycleStatus_2 = Convert.ToInt32(ft2["lifecyclestatus"]);
                                                string lifecycle_jun = cls_required_methods.GetLifecyclestatus(fcljun, "lifecyclestatus", int.Parse(pointLifecycleStatus_2.ToString()));

                                                sw.WriteLine($"{fcldev.GetName()}, {dev_assgrpdesc}, {dev_asstypdesc}, {globalid_dev}, {uniqueid_dev}, {lifecycle_dev},{fcljun.GetName()}, {jun_assgrpdesc}, {jun_asstypdesc}, {globalid_jun}, {uniqueid_jun}, {lifecycle_jun},{distance.ToString("F40")}, {"Stacked Device and Junction"}");

                                                string ag_at1 = dicsubtype_dev[Convert.ToInt32(assegrop1)] + "_" + astype1;
                                                string ag_at2 = dicsubtype_jun[Convert.ToInt32(assegrop2)] + "_" + astype2;
                                                string ag_at_ag_at1 = ag_at1 + "|" + ag_at2;

                                                if (dic_stack_cnt.ContainsKey(ag_at_ag_at1))
                                                {
                                                    dic_stack_cnt[ag_at_ag_at1] = dic_stack_cnt[ag_at_ag_at1] + 1;
                                                }
                                                else
                                                {
                                                    dic_stack_cnt.Add(ag_at_ag_at1, 1);
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception innerEx)
                                    {
                                        Console.WriteLine("Error processing feature pair: " + innerEx.Message);
                                    }
                                }
                            }
                            catch (Exception midEx)
                            {
                                Console.WriteLine("Error processing feature: " + midEx.Message);
                            }
                        }
                    }

                    sw.WriteLine("EndTime : " + System.DateTime.Now.ToString("yyyyMMdd") + "_" + System.DateTime.Now.ToString("HHmmss"));
                    sw.Close();
                    lblpro.Content = "Process Completed";
                    System.Windows.Forms.Application.DoEvents();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error occurred in the process: " + ex.Message);
                }
            }
        }
    }
}
