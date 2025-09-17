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
    class Report_different_feeder_lines_intersecting
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
            string rptName_1 = $"{"Report_Different_Feeder_Intersecting_Line"}_{timestamp}.csv";
            StreamWriter stw_1 = new StreamWriter(System.IO.Path.Combine(Dockpane1View.reportpath, rptName_1));

            string rptName_2 = $"{"Report_Junction_at_intersection_of_different_feeder_lineend"}_{timestamp}.csv";
            StreamWriter stw_2 = new StreamWriter(System.IO.Path.Combine(Dockpane1View.reportpath, rptName_2));

            await QueuedTask.Run(() =>
            {
                stw_1.WriteLine(
        "FeatureClass_Line1,AssetGroup_Line1,AssetType_Line1,GlobalID_Line1,UniqueID_Line1,FeederID_Line1," +
        "AssetGroup_Line2,AssetType_Line2,GlobalID_Line2,UniqueID_Line2,FeederID_Line2,Remarks");

                stw_2.WriteLine(
                    "FeatureClass_Line1,AssetGroup_Line1,AssetType_Line1,GlobalID_Line1,UniqueID_Line1,FeederID_Line1," +
                    "AssetGroup_Line2,AssetType_Line2,GlobalID_Line2,UniqueID_Line2,FeederID_Line2," +
                    "AssetGroup_Junction,AssetType_Junction,Globalid_Junction,UniqueID_Junction,Remarks"
                );

                report_connected_lines_of_different_feeder(lblpro, stw_1, uniqueid, stw_2);
            });
            endTime = DateTime.Now;
            cls_required_methods.writelogtimetocsv("log_reportdifferentfeederintersecting", "Report intersecting lines of different feeder", endTime, startTime);
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
        public static void Is_Connected_To_differentFeeder(ReadOnlyPointCollection points, long oid, FeatureClass fcl, string legacy_feederid_1, Feature lineft_1, Dictionary<int, string> dic_subtype_line_1, string uniqueid, StreamWriter stw, FeatureClass devfc)
        {

            foreach (MapPoint pt_featshape in points)
            {

                bool flag = false;
                SpatialQueryFilter sf = new SpatialQueryFilter();
                sf.SubFields = $"objectid,shape,assetgroup,assettype,globalid,legacy_feederid,{uniqueid}";
                sf.FilterGeometry = GeometryEngine.Instance.Buffer(pt_featshape, UNTolerance);
                sf.SpatialRelationship = SpatialRelationship.Intersects;
                sf.WhereClause = $"objectid NOT IN({oid}) AND legacy_feederid NOT IN ('{legacy_feederid_1}') ";
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
                                string l_feederid_2 = ft_cr_feat["legacy_feederid"] == null || ft_cr_feat["legacy_feederid"] == DBNull.Value ? "Null" : ft_cr_feat["legacy_feederid"].ToString();
                                if (l_feederid_2 != legacy_feederid_1)
                                {
                                    if(!IsDevice_Exist(devfc, pt_featshape))
                                    {
                                        flag = true;
                                        List<string> lst_line_details_1 = get_feature_details(fcl, lineft_1, dic_subtype_line_1, uniqueid);
                                        List<string> lst_line_details_2 = get_feature_details(fcl, ft_cr_feat, dic_subtype_line_1, uniqueid);
                                        stw.WriteLine(
        $"ElectricLine," +
        $"{lst_line_details_1[0]},{lst_line_details_1[1]},{lst_line_details_1[2]},{lst_line_details_1[3]},{legacy_feederid_1}," +
        $"{lst_line_details_2[0]},{lst_line_details_2[1]},{lst_line_details_2[2]},{lst_line_details_2[3]},{l_feederid_2}," +
        $"Lines of different feeders intersected without device");
                                        break;
                                    }
                                   
                                    
                                }

                            }

                        }
                    }
                }

            }

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

        public static void report_connected_lines_of_different_feeder(System.Windows.Controls.Label lblpro, StreamWriter sw, string uniqueid, StreamWriter sw_2)
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
                        string l_feederid_1 = ft_line["legacy_feederid"] == null || ft_line["legacy_feederid"] == DBNull.Value ? "Null" : ft_line["legacy_feederid"].ToString();
                        lblpro.Dispatcher.Invoke(() => lblpro.Content = $"Processing Line: {num_crcount} / {cntx_max}");

                        Is_Connected_To_differentFeeder(ptcollx, ft_line.GetObjectID(), linefc, l_feederid_1, ft_line, dic_subtype_line, uniqueid, sw, devfc);
                        Is_EndPoint_Connected_To_DifferentFeeder(
    linefc, ft_line, l_feederid_1,
    dic_subtype_line, uniqueid, sw_2, junfc);
                    }

                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Error occurred: " + ex.Message);
                }
                finally
                {
                    sw.Close();
                    sw.Dispose();
                    sw_2.Close();
                    sw_2.Dispose();
                }

            }



        }
        public static bool IsDevice_Exist(FeatureClass devfc, MapPoint pt_shape)
        {
            bool flag = false;
            SpatialQueryFilter sf = new SpatialQueryFilter();
            sf.SubFields = "objectid,shape,assetgroup,assettype,globalid";
            sf.FilterGeometry = GeometryEngine.Instance.Buffer(pt_shape, UNTolerance);
            sf.SpatialRelationship = SpatialRelationship.Intersects;
            var fcur = devfc.Search(sf, true);
            while (fcur.MoveNext())
            {
                Feature ft_cr_feat = fcur.Current as Feature;
                if (GeometryEngine.Instance.Distance(ft_cr_feat.GetShape(), pt_shape) <= UNTolerance)
                {
                    MapPoint pt_cr_feat_shape = ft_cr_feat.GetShape() as MapPoint;
                    if (pt_cr_feat_shape.Z.ToString("0.00") == pt_shape.Z.ToString("0.00"))
                    {
                        flag = true;
                        break;
                    }

                }
            }

            return flag;
        }
        public static void Is_EndPoint_Connected_To_DifferentFeeder(FeatureClass fcl, Feature lineft_1, string legacy_feederid_1, Dictionary<int, string> dic_subtype_line_1, string uniqueid, StreamWriter stw, FeatureClass junfc)
        {
            Dictionary<int, string> dic_subtype_junctions = Get_subtype_code_val(junfc);
            // Get endpoints of line1
            Polyline pl1 = lineft_1.GetShape() as Polyline;
            if (pl1 == null || pl1.PointCount < 2) return;

            MapPoint start1 = pl1.Points.First();
            MapPoint end1 = pl1.Points.Last();

            List<MapPoint> endpoints1 = new List<MapPoint> { start1, end1 };

            foreach (var pt in endpoints1)
            {
                SpatialQueryFilter sf = new SpatialQueryFilter
                {
                    SubFields = $"objectid,shape,assetgroup,assettype,globalid,legacy_feederid,{uniqueid}",
                    FilterGeometry = GeometryEngine.Instance.Buffer(pt, UNTolerance),
                    SpatialRelationship = SpatialRelationship.Intersects,
                    WhereClause = $"objectid <> {lineft_1.GetObjectID()} AND legacy_feederid NOT IN ('{legacy_feederid_1}')"
                };

                using (RowCursor fcur = fcl.Search(sf, true))
                {
                    while (fcur.MoveNext())
                    {
                        Feature lineft_2 = fcur.Current as Feature;
                        if (lineft_2 == null) continue;

                        Polyline pl2 = lineft_2.GetShape() as Polyline;
                        if (pl2 == null || pl2.PointCount < 2) continue;

                        MapPoint start2 = pl2.Points.First();
                        MapPoint end2 = pl2.Points.Last();

                        if ((GeometryEngine.Instance.Distance(pt, start2) <= UNTolerance &&
                             pt.Z.ToString("0.00") == start2.Z.ToString("0.00"))
                            ||
                            (GeometryEngine.Instance.Distance(pt, end2) <= UNTolerance &&
                             pt.Z.ToString("0.00") == end2.Z.ToString("0.00")))
                        {
                            // Find junction(s) at this point
                            SpatialQueryFilter sfJunction = new SpatialQueryFilter
                            {
                                SubFields = $"objectid,shape,assetgroup,assettype,globalid,{uniqueid}",
                                FilterGeometry = GeometryEngine.Instance.Buffer(pt, UNTolerance),
                                SpatialRelationship = SpatialRelationship.Intersects
                            };

                            using (RowCursor juncCur = junfc.Search(sfJunction, true))
                            {
                                while (juncCur.MoveNext())
                                {
                                    Feature junction = juncCur.Current as Feature;
                                    if (junction == null) continue;

                                    if (GeometryEngine.Instance.Distance(junction.GetShape(), pt) <= UNTolerance)
                                    {
                                        MapPoint pt_cr_feat_shape = junction.GetShape() as MapPoint;

                                        if (pt_cr_feat_shape.Z.ToString("0.00") == pt.Z.ToString("0.00"))
                                        {
                                            string feeder2 = lineft_2["legacy_feederid"]?.ToString() ?? "Null";
                                            List<string> lst_line_details_1 = get_feature_details(fcl, lineft_1, dic_subtype_line_1, uniqueid);
                                            List<string> lst_line_details_2 = get_feature_details(fcl, lineft_2, dic_subtype_line_1, uniqueid);

                                            List<string> junction_details = get_feature_details(junfc, junction, dic_subtype_junctions, uniqueid);

                                            stw.WriteLine(
    $"ElectricLine," +
    $"{lst_line_details_1[0]},{lst_line_details_1[1]},{lst_line_details_1[2]},{lst_line_details_1[3]},{legacy_feederid_1}," +
    $"{lst_line_details_2[0]},{lst_line_details_2[1]},{lst_line_details_2[2]},{lst_line_details_2[3]},{feeder2}," +
    $"{junction_details[0]},{junction_details[1]},{junction_details[2]},{junction_details[3]}," +
    $"Junction is present at transition point of different feeders"
);
                                        }
                                    }
                                }



                            }
                        }
                    }
                }
            }


        }
    }
}
