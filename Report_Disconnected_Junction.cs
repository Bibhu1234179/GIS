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
    class Report_Disconnected_Junction
    {
        public static void Process(System.Windows.Controls.Label lblpro)
        {
            string uniqueid = MainWindow.uniquefield;
            DateTime currentTime = DateTime.Now;
            string timestamp = currentTime.ToString("yyyy-MM-dd_HH-mm-ss");
            string rptName = $"{"Report_Disconnected_Junction"}_{timestamp}.csv";
            StreamWriter stw = new StreamWriter(System.IO.Path.Combine(MainWindow.reportpath, rptName));
            stw.WriteLine("FeatureClass_Junction, AssetGroup_Junction, AssetType_Junction, GlobalID_Junction, UniqueID_Junction, Remarks");
            ReportDisConnectedJunction(lblpro, uniqueid, stw);
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
        public static bool FieldExists(FeatureClass featureClass, string fieldName)
        {
            FeatureClassDefinition def = featureClass.GetDefinition();
            return def.GetFields().Any(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
        }
        public static List<string> get_feature_details(FeatureClass Featureclass, Feature ftline, Dictionary<int, string> dic_subtype_line, string str_UniqueidField)
        {
            string str_asg_code = ftline["assetgroup"].ToString();
            string str_asg_desc = dic_subtype_line[Convert.ToInt32(str_asg_code)];
            string str_ast_code = ftline["assettype"].ToString();
            Domain dom_ast1 = GetDomainFromField(Featureclass, "assettype", str_asg_desc);
            string str_astdesc = GetDomainDesc(dom_ast1, Convert.ToInt32(str_ast_code));
            string globalid1 = ftline["Globalid"].ToString();
            string uniqueid1 = null;
            if (FieldExists(Featureclass, str_UniqueidField))
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
        public static void ReportDisConnectedJunction(System.Windows.Controls.Label lblpro, string uniqueid, StreamWriter sw)
        {
            List<string> disconnectedJunctionReport = new List<string>();

            try
            {
                Geodatabase sourcegdb = ClassRequiredDetails.Geodatabase;

                Table domnettab = sourcegdb.OpenDataset<Table>("A_DomainNetwork");
                string domainstring = GetDomainNetwork(domnettab);

                FeatureClass Junctionclass = sourcegdb.OpenDataset<FeatureClass>(domainstring + "Junction");
                FeatureClass Lineclass = sourcegdb.OpenDataset<FeatureClass>(domainstring + "Line");

                double Untolerence = MainWindow.UNTolerance;
                Dictionary<int, string> dicsubtype_jun = Get_subtype_code_val(Junctionclass);
                string fcnamee = Junctionclass.GetName();

                int cnt = 0;
                var totalCount = Junctionclass.GetCount();

                using (RowCursor fcur_junction = Junctionclass.Search(null, false))
                {
                    while (fcur_junction.MoveNext())
                    {
                        Feature junFt = fcur_junction.Current as Feature;
                        MapPoint pnegeo = junFt.GetShape() as MapPoint;

                        lblpro.Content = $"Processing Junction: {cnt++}/{totalCount}";
                        System.Windows.Forms.Application.DoEvents();

                        bool IsConnectedJunction = false;

                        SpatialQueryFilter sf = new SpatialQueryFilter
                        {
                            FilterGeometry = GeometryEngine.Instance.Buffer(pnegeo, Untolerence),
                            SpatialRelationship = SpatialRelationship.Intersects
                        };

                        using (RowCursor fcur_line2 = Lineclass.Search(sf, false))
                        {
                            while (fcur_line2.MoveNext())
                            {
                                Feature lineFt = fcur_line2.Current as Feature;
                                Polyline LineGeo = lineFt.GetShape() as Polyline;
                                bool ISconnectFlag = false;

                                for (int i = 0; i < LineGeo.PointCount; i++)
                                {
                                    if (GeometryEngine.Instance.Distance(pnegeo, LineGeo.Points[i]) <= Untolerence)
                                    {
                                        if (pnegeo.Z.ToString("0.00") == LineGeo.Points[i].Z.ToString("0.00"))
                                        {
                                            ISconnectFlag = true;
                                            IsConnectedJunction = true;
                                            break;
                                        }
                                    }
                                }

                                if (ISconnectFlag)
                                    break;
                            }
                        }

                        if (!IsConnectedJunction)
                        {
                            List<string> lst_dev_details = get_feature_details(Junctionclass, junFt, dicsubtype_jun, uniqueid);
                            disconnectedJunctionReport.Add(
                                $"{Junctionclass.GetName()},{lst_dev_details[0]},{lst_dev_details[1]},{lst_dev_details[2]},{lst_dev_details[3]},Junction is Disconnected"
                            );
                        }
                    }
                }

               
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblpro.Content = "Error occurred during processing.";
            }
            finally
            {
                if (sw != null)
                {
                    foreach (string line in disconnectedJunctionReport)
                    {
                        sw.WriteLine(line);
                    }

                    sw.Close();
                    sw.Dispose();
                }
                lblpro.Content = "Process Completed!";
                System.Windows.Forms.Application.DoEvents();
            }
        }
    }
}
