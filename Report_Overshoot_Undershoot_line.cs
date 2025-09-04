using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Build_Connectivity.Tools.QC_Tools
{
    class Report_Overshoot_Undershoot
    {
        public static double shootsTolerance = 0.05;
        public static void Process(System.Windows.Controls.Label lblpro)
        {
            string uniqueid = MainWindow.uniquefield;
            DateTime currentTime = DateTime.Now;
            string timestamp = currentTime.ToString("yyyy-MM-dd_HH-mm-ss");
            string rptName = $"{"Report_Overshoot_Undershoot"}_{timestamp}.csv";
            StreamWriter sw = new StreamWriter(System.IO.Path.Combine(MainWindow.reportpath, rptName));
            sw.WriteLine("Fc_Name, Asset_Group_1, Asset_Type_1, Global_ID_1, UniqueId_1, Asset_Group_2, Asset_Type_2, Global_ID_2, UniqueId_2, Distance, Remarks");
            Report_overshootundershoot(lblpro, sw, uniqueid );
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
        public static List<string> get_feature_details(FeatureClass featureClass, Feature ftline, Dictionary<int, string> dic_subtype_line, string str_UniqueidField)
        {
            string str_asg_code = ftline["assetgroup"].ToString();
            string str_asg_desc = dic_subtype_line[Convert.ToInt32(str_asg_code)];
            string str_ast_code = ftline["assettype"].ToString();
            Domain dom_ast1 = GetDomainFromField(featureClass, "assettype", str_asg_desc);
            string str_astdesc = GetDomainDesc(dom_ast1, Convert.ToInt32(str_ast_code));
            string globalid1 = ftline["Globalid"].ToString();
            string uniqueid1 = null;
            if (FieldExists(featureClass, str_UniqueidField))
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
        public static bool FieldExists(FeatureClass featureClass, string fieldName)
        {
            FeatureClassDefinition def = featureClass.GetDefinition();
            return def.GetFields().Any(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
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
        public static void Report_overshootundershoot(System.Windows.Controls.Label lblpro, StreamWriter sw, String str_Uniqueid_field)
        {
            Geodatabase sourcegdb = ClassRequiredDetails.Geodatabase;
            HashSet<string> Unique_comb = new HashSet<string>();
            List<string> Report_items = new List<string>();
            Table domnettab = sourcegdb.OpenDataset<Table>("A_DomainNetwork");
            string domainstring = GetDomainNetwork(domnettab);
            long ob = 0;
            FeatureClass Fc_Lineclass = sourcegdb.OpenDataset<FeatureClass>(domainstring + "Line"); 
            HashSet<string> set_unique_combination = new HashSet<string>();
            try
            {
                string str_subfields = "objectid,shape,assetgroup,assettype,globalid";

                if (!str_subfields.Split(',').Contains(str_Uniqueid_field))
                {
                    str_subfields += "," + str_Uniqueid_field;
                }

              
                Dictionary<int, string> dict_subtype_codedesc_line = Get_subtype_code_val(Fc_Lineclass);
                string Lineclass_name = Fc_Lineclass.GetName();

                ArcGIS.Core.Data.QueryFilter q_filter = new ArcGIS.Core.Data.QueryFilter();
                q_filter.SubFields = str_subfields;

                int num_current_iteration = 0;
                long num_max_iteration = Fc_Lineclass.GetCount(q_filter);
                var cursor_Line = Fc_Lineclass.Search(q_filter, false);

                while (cursor_Line.MoveNext())
                {
                    num_current_iteration++;
                    lblpro.Content = $"Processing Line: {num_current_iteration}/{num_max_iteration}";
                    System.Windows.Forms.Application.DoEvents();

                    Feature line1_feature = cursor_Line.Current as Feature;
                    ArcGIS.Core.Geometry.Polyline Line1_Shape = line1_feature.GetShape() as ArcGIS.Core.Geometry.Polyline;
                    MapPoint From_Point = Line1_Shape.Points[0];
                    MapPoint To_Point = Line1_Shape.Points[Line1_Shape.PointCount - 1];
                    ob = line1_feature.GetObjectID();
                    List<string> lst_Line1_details = get_feature_details(Fc_Lineclass, line1_feature, dict_subtype_codedesc_line, str_Uniqueid_field);

                    Feature from_ft = Get_Disconnect_Line_Ft(Fc_Lineclass, str_subfields, From_Point, Line1_Shape, line1_feature.GetObjectID(), out MapPoint frm_vertex);
                    Feature to_ft = Get_Disconnect_Line_Ft(Fc_Lineclass, str_subfields, To_Point, Line1_Shape, line1_feature.GetObjectID(), out MapPoint to_vertex);
                    if(from_ft != null)
                    {
                        List<long> lst_ft_oid = new List<long>() { line1_feature.GetObjectID(), from_ft.GetObjectID() };
                        lst_ft_oid.Sort();
                        if (!set_unique_combination.Add(lst_ft_oid[0] + "_" + lst_ft_oid[1])) continue;
                        double from_distance = GeometryEngine.Instance.Distance(From_Point, frm_vertex);
                        List<string> lst_frmFt_details = get_feature_details(Fc_Lineclass, from_ft, dict_subtype_codedesc_line, str_Uniqueid_field);

                        Report_items.Add($"{Lineclass_name},{lst_Line1_details[0]},{lst_Line1_details[1]},{lst_Line1_details[2]},{lst_Line1_details[3]}," +
                            $"{lst_frmFt_details[0]},{lst_frmFt_details[1]},{lst_frmFt_details[2]},{lst_frmFt_details[3]},{from_distance},Line End Point Is Disconnected");
                    }
                    if (to_ft != null)
                    {
                        List<long> lst_ft_oid = new List<long>() { line1_feature.GetObjectID(), to_ft.GetObjectID() };
                        lst_ft_oid.Sort();
                        if (!set_unique_combination.Add(lst_ft_oid[0] + "_" + lst_ft_oid[1])) continue;

                        double to_distance = GeometryEngine.Instance.Distance(To_Point, to_vertex);
                        List<string> lst_toFt_details = get_feature_details(Fc_Lineclass, to_ft, dict_subtype_codedesc_line, str_Uniqueid_field);
                        Report_items.Add($"{Lineclass_name},{lst_Line1_details[0]},{lst_Line1_details[1]},{lst_Line1_details[2]},{lst_Line1_details[3]}," +
                            $"{lst_toFt_details[0]},{lst_toFt_details[1]},{lst_toFt_details[2]},{lst_toFt_details[3]},{to_distance},Line Start Point Is Disconnected");
                    }
                }
            }
            catch (Exception ecs)
            {
                lblpro.Content = "Error: " + ecs.Message;
                System.Windows.Forms.MessageBox.Show("Error occurred: " + ecs.ToString());
            }
            finally
            {
                sw.AutoFlush = true;
                foreach (string item in Report_items)
                {
                    sw.WriteLine(item);
                }
                sw.Close();
                sw.Dispose();
            }
            lblpro.Content = "Process Completed!";
            System.Windows.Forms.Application.DoEvents();
        }
        private static bool IsLines_Connected(Polyline line1, Polyline line2)
        {
            for (int i = 0; i < line1.PointCount; i++)
            {
                MapPoint Point_Geometry = line1.Points[i];

                for (int j = 0; j < line2.PointCount; j++)
                {
                    if (Point_Geometry.Z.ToString("0.00") == line2.Points[j].Z.ToString("0.00"))
                    {
                        if (GeometryEngine.Instance.Distance(Point_Geometry, line2.Points[j]) == 0)
                        {
                            return true;
                        }
                    }
                }

            }
            return false;
        }
        private static bool Is_Point_Connected_Lines(MapPoint line1_Point, List<Feature> lst_Features)
        {
            MapPoint Point_Geometry = line1_Point;
            foreach (Feature Feature in lst_Features)
            {
                Polyline line2 = Feature.GetShape() as Polyline;
                for (int j = 0; j < line2.PointCount; j++)
                {
                    if (Point_Geometry.Z.ToString("0.00") == line2.Points[j].Z.ToString("0.00"))
                    {
                        if (GeometryEngine.Instance.Distance(Point_Geometry, line2.Points[j]) == 0)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }


        public static Feature Get_Disconnect_Line_Ft(FeatureClass Ft_class, string str_subfields, MapPoint Point_Geometry, Polyline line_geometry, long oid, out MapPoint disconnectvetex)
        {
            disconnectvetex = null;
            Feature Disconnect_ft = null;
            List<Feature> Snaping_Feats = new List<Feature>();
            bool is_over_under_shoot = false;
            SpatialQueryFilter spatialQueryFilter = new SpatialQueryFilter();
            spatialQueryFilter.SubFields = str_subfields;
            spatialQueryFilter.FilterGeometry = GeometryEngine.Instance.Buffer(Point_Geometry, MainWindow.UNTolerance);
            spatialQueryFilter.SpatialRelationship = SpatialRelationship.Intersects;
            spatialQueryFilter.WhereClause = $"objectid NOT IN({oid})";
            var cursor_Line = Ft_class.Search(spatialQueryFilter, false);

            while (cursor_Line.MoveNext())
            {
                Feature ft_line = cursor_Line.Current as Feature;
                Snaping_Feats.Add(ft_line);
                #region unused
                //Polyline pl_currentline = ft_line.GetShape() as Polyline;
                //for (int i = 0; i < pl_currentline.PointCount; i++)
                //{
                //    if (GeometryEngine.Instance.Distance(Point_Geometry, pl_currentline.Points[i]) < buffer_tolrence)
                //    {
                //        if (Point_Geometry.Z.ToString("0.00") == pl_currentline.Points[i].Z.ToString("0.00"))
                //        {
                //            if (GeometryEngine.Instance.Distance(Point_Geometry, pl_currentline.Points[i]) != 0)
                //            {
                //                //if (IsLinesAre_Connected(Point_Geometry, pl_currentline)) continue;
                //                if (IsLines_Connected(line_geometry, pl_currentline)) continue;
                //                disconnectvetex = pl_currentline.Points[i];
                //                return ft_line;
                //            }
                //        }


                //    }
                //}
                #endregion

            }
            if (!Is_Point_Connected_Lines(Point_Geometry, Snaping_Feats))
            {
                foreach (Feature Feature in Snaping_Feats)
                {
                    Polyline pl_currentline = Feature.GetShape() as Polyline;
                    for (int i = 0; i < pl_currentline.PointCount; i++)
                    {
                        if (GeometryEngine.Instance.Distance(Point_Geometry, pl_currentline.Points[i]) < shootsTolerance)
                        {
                            if (Point_Geometry.Z.ToString("0.00") == pl_currentline.Points[i].Z.ToString("0.00"))
                            {
                                if (GeometryEngine.Instance.Distance(Point_Geometry, pl_currentline.Points[i]) != 0)
                                {
                                    //if (IsLinesAre_Connected(Point_Geometry, pl_currentline)) continue;
                                    if (IsLines_Connected(line_geometry, pl_currentline)) continue;
                                    disconnectvetex = pl_currentline.Points[i];
                                    return Feature;
                                }
                            }


                        }
                    }
                }
            }
            return null;
        }

    }
}
