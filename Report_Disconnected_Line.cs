using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QC_Tools.Tools
{
    class Report_Disconnected_Line
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
            string rptName = $"{"Report_Disconnected_Line"}_{timestamp}.csv";
            StreamWriter stw = new StreamWriter(System.IO.Path.Combine(Dockpane1View.reportpath, rptName));


            await QueuedTask.Run(() =>
            {
                stw.WriteLine("FeatureClass_Line, AssetGroup_Line, AssetType_Line, GlobalID_Line, UniqueID_Line, Remarks");
                report_disconnected_line(lblpro, stw, uniqueid);
            });
            endTime = DateTime.Now;
            cls_required_methods.writelogtimetocsv("log_reportdisconnedtedlines", "Report Disconnected Line", endTime, startTime);
        }
        public static Dictionary<int, string> Get_subtype_code_val(FeatureClass Fc_reqfeatureclass)
        {
            Dictionary<int, string> dict_subtype_code_desc = new Dictionary<int, string>();
            IReadOnlyList<Subtype> lst_subtypelist_dev = Fc_reqfeatureclass.GetDefinition().GetSubtypes();
            foreach (Subtype xx in lst_subtypelist_dev)
            {
                dict_subtype_code_desc.Add(xx.GetCode(), xx.GetName());
            }
            return dict_subtype_code_desc;
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
        public static string GetDomainDesc(Domain domain, int num_domaincode)
        {
            string str_description = "";
            try
            {
                if (domain is CodedValueDomain code_dmain)
                {

                    Dictionary<object, string> list = new Dictionary<object, string>();
                    SortedList<object, string> codedValuePairs = code_dmain.GetCodedValuePairs();

                    foreach (var item in codedValuePairs)
                    {
                        if (item.Key.ToString() == num_domaincode.ToString())
                        {
                            str_description = item.Value;
                            return str_description;
                        }
                    }
                }
                else
                {
                    return str_description;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message.ToString());
            }
            return str_description;
        }
        public static bool Is_Snapped_With_Device(FeatureClass fcl_devclass, MapPoint pt_featshape)
        {
            bool flag = false;
            //TransFt = null;
            SpatialQueryFilter sf = new SpatialQueryFilter();
            sf.FilterGeometry = GeometryEngine.Instance.Buffer(pt_featshape, UNTolerance); // 5 ft is aaproximately 1.524 meter
            sf.SpatialRelationship = SpatialRelationship.Intersects;
            //sf.WhereClause = $"assetgroup IN({str_transformer_ag})";
            var fcur = fcl_devclass.Search(sf, false);
            while (fcur.MoveNext())
            {
                Feature ft_device = fcur.Current as Feature;
                if (GeometryEngine.Instance.Distance(ft_device.GetShape(), pt_featshape) <=UNTolerance)
                {
                    MapPoint mpoint = ft_device.GetShape() as MapPoint;
                    if (mpoint.Z.ToString("0.00") == pt_featshape.Z.ToString("0.00"))
                    {
                        flag = true;
                        //TransFt = ft_device;
                        return flag;
                    }
                }
            }
            return flag;
        }
        public static bool Is_Snapped_With_Junction(FeatureClass fcl_junclass, MapPoint pt_featshape)
        {
            bool flag = false;
            //TransFt = null;
            SpatialQueryFilter sf = new SpatialQueryFilter();
            sf.FilterGeometry = GeometryEngine.Instance.Buffer(pt_featshape, UNTolerance); // 5 ft is aaproximately 1.524 meter
            sf.SpatialRelationship = SpatialRelationship.Intersects;
            //sf.WhereClause = $"assetgroup IN({str_transformer_ag})";
            var fcur = fcl_junclass.Search(sf, false);
            while (fcur.MoveNext())
            {
                Feature ft_device = fcur.Current as Feature;
                if (GeometryEngine.Instance.Distance(ft_device.GetShape(), pt_featshape) <=UNTolerance)
                {
                    MapPoint mpoint = ft_device.GetShape() as MapPoint;
                    if (mpoint.Z.ToString("0.00") == pt_featshape.Z.ToString("0.00"))
                    {
                        flag = true;
                        //TransFt = ft_device;
                        return flag;
                    }
                }
            }
            return flag;
        }
        public static bool Is_Snapped_With_Line(ReadOnlyPointCollection points, long oid, FeatureClass fcl)
        {
            
            foreach (MapPoint pt_featshape in points )
            {
               
                bool flag = false;
                SpatialQueryFilter sf = new SpatialQueryFilter();
                sf.SubFields = "objectid,shape,assetgroup,assettype,globalid";
                sf.FilterGeometry = GeometryEngine.Instance.Buffer(pt_featshape, UNTolerance);
                sf.SpatialRelationship = SpatialRelationship.Intersects;
                sf.WhereClause = $"objectid NOT IN({oid})";
                var fcur = fcl.Search(sf, true);

                while (fcur.MoveNext())
                {
                    Feature ft_cr_feat = fcur.Current as Feature;
                    //int assettype_2 = Convert.ToInt32(ft_cr_feat["assettype"]);
                    //int assetgroup_2 = Convert.ToInt32(ft_cr_feat["assetgroup"]);
                    //string line_asstypdesc = ClassRequiredDetails.GetAssetTypeDescription(ClassRequiredDetails.Lineclass, assetgroup_2, assettype_2, out string line_assgrpdesc);
                    Polyline pl = ft_cr_feat.GetShape() as Polyline;
                    for (int i = 0; i < pl.PointCount; i++)
                    {
                        if (GeometryEngine.Instance.Distance(pt_featshape, pl.Points[i]) <= UNTolerance)
                        {
                            if ((pt_featshape.Z.ToString("0.00") == pl.Points[i].Z.ToString("0.00")))
                            {
                                return true;
                            }

                        }
                    }
                }
                
            }
            return false;


           
        }
        public static Domain GetDomainFromField(FeatureClass fcldev, string fieldname, string subtypedesc)
        {
            IReadOnlyList<Subtype> subtypelist_dev = fcldev.GetDefinition().GetSubtypes();
            TableDefinition tableDefinition = fcldev.GetDefinition();
            Subtype subtype = tableDefinition.GetSubtypes().First(x => x.GetName().ToLower() == subtypedesc.ToLower());
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
        public static bool FieldExists(FeatureClass featureClass, string fieldName)
        {
            FeatureClassDefinition def = featureClass.GetDefinition();
            return def.GetFields().Any(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
        }
        public static List<string> get_feature_details(FeatureClass Fc_Lineclass, Feature ftline, Dictionary<int, string> dic_subtype_line, string str_UniqueidField)
        {
            string str_asg_code = ftline["assetgroup"].ToString();
            string str_asg_desc = dic_subtype_line[Convert.ToInt32(str_asg_code)];
            string str_ast_code = ftline["assettype"].ToString();
            Domain dom_ast1 = GetDomainFromField(Fc_Lineclass, "assettype", str_asg_desc);
            string str_astdesc = GetDomainDesc(dom_ast1, Convert.ToInt32(str_ast_code));
            string globalid1 = ftline["Globalid"].ToString();
            string uniqueid1 = null;
            if (FieldExists(Fc_Lineclass, str_UniqueidField))
            {
                uniqueid1 = ftline[str_UniqueidField] == null || ftline[str_UniqueidField] == DBNull.Value ? "null" : ftline[str_UniqueidField].ToString();
            }
            else
            {
                uniqueid1 = ftline.GetObjectID().ToString();
            }

            List<string> lst_ft_details = new List<string>() { str_asg_desc, str_astdesc, globalid1.Trim(), uniqueid1 };
            return lst_ft_details;

        }
        public static void report_disconnected_line(System.Windows.Controls.Label lblpro, StreamWriter sw, string uniqueid)
        {
            List<string> disconnectedLinesReport = new List<string>();
            int num_crcount = 0;
            using (Geodatabase sourcegdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(Dockpane1View.gdbpath))))
            {
                try
                {

                    Table domnettab = sourcegdb.OpenDataset<Table>("A_DomainNetwork");
                    string domainstring = GetDomainNetwork(domnettab);

                    FeatureClass linefc = sourcegdb.OpenDataset<FeatureClass>(domainstring + "Line");
                    FeatureClass devfc = sourcegdb.OpenDataset<FeatureClass>(domainstring + "Device");
                    FeatureClass junfc = sourcegdb.OpenDataset<FeatureClass>(domainstring + "Junction");
                    if (!(FieldExists(linefc, uniqueid)))
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Provided unique id field is not valid");
                        return;
                    }
                    double xytolerance = devfc.GetDefinition().GetSpatialReference().XYTolerance;
                    UNTolerance = xytolerance * 2 * Math.Sqrt(2);

                    Dictionary<int, string> dic_subtype_line = Get_subtype_code_val(linefc);
                    long cntx_max = linefc.GetCount();


                    var fcurline = linefc.Search(null, false);

                    while (fcurline.MoveNext())
                    {
                        Feature ft_line = fcurline.Current as Feature;
                        num_crcount++;

                        Polyline plx = ft_line.GetShape() as Polyline;
                        ReadOnlyPointCollection ptcollx = plx.Points;

                        MapPoint startPt = ptcollx[0];
                        MapPoint endPt = ptcollx[ptcollx.Count - 1];

                        bool fromDisconnected;
                        bool toDisconnected;
                        bool eachpointDisconnected;

                        lblpro.Dispatcher.Invoke(() => lblpro.Content = $"Processing Line: {num_crcount} / {cntx_max}");
                       

                        if (Is_Snapped_With_Device(devfc, startPt))
                        {
                            continue;
                        }
                        else if (Is_Snapped_With_Junction(junfc, startPt))
                        {
                            continue;
                        }                      
                        else
                        {
                            fromDisconnected = true;
                        }

                        if (Is_Snapped_With_Device(devfc, endPt))
                        {
                            continue;
                        }
                        else if (Is_Snapped_With_Junction(junfc, endPt))
                        {
                            continue;
                        }                       
                        else
                        {
                            toDisconnected = true;
                        }

                        if(Is_Snapped_With_Line(ptcollx, ft_line.GetObjectID(), linefc))
                        {
                            eachpointDisconnected = true;
                        }
                        else
                        {
                            eachpointDisconnected = false;
                        }


                        if (fromDisconnected && toDisconnected && eachpointDisconnected)
                        {
                            List<string> lst_line_details = get_feature_details(linefc, ft_line, dic_subtype_line, uniqueid);

                            disconnectedLinesReport.Add(
                                $"{linefc.GetName()},{lst_line_details[0]},{lst_line_details[1]},{lst_line_details[2]},{lst_line_details[3]},line is not snapped to any feature"
                            );
                        }
                        //lblpro.Dispatcher.Invoke(() => lblpro.Content = $"Processing Line: {num_crcount} / {cntx_max}");
                    
                    }

                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Error occurred: " + ex.Message);
                }
                finally
                {
                    // Write all collected results to the StreamWriter
                    foreach (var line in disconnectedLinesReport)
                    {
                        sw.WriteLine(line);
                    } // Ensure all data is written
                    sw.Close();
                    sw.Dispose();
                }

            }
                


        }

    }
}
