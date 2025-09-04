using ArcGIS.Core.Data;
using ArcGIS.Core.Data.UtilityNetwork;
using ArcGIS.Core.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Build_Connectivity.Tools.QC_Tools
{
    class Report_lines_With_Same_Voltage_Group
    {
        public static string fld_opt_voltage = MainWindow.voltagefield;
        public static void Process(System.Windows.Controls.Label lblpro)
        {
            string uniqueid = MainWindow.uniquefield;
            DateTime currentTime = DateTime.Now;
            string timestamp = currentTime.ToString("yyyy-MM-dd_HH-mm-ss");
            string rptName = $"{"Report_ST_Connected_Lines_Of_Same_Voltage"}_{timestamp}.csv";
            StreamWriter stw = new StreamWriter(System.IO.Path.Combine(MainWindow.reportpath, rptName));
            stw.WriteLine("FeatureClass, AssetGroup, AssetType, GlobalID, UniqueID, Remarks");
            Report_Sb_Sp_Trns_Connect_To_Line(stw, lblpro, uniqueid);
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
                    // Get subtype for this row

                }

                // Get the coded value domain for this field
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
        public static string get_terminal_whereclause(List<string> lst_combination)
        {
            string str_terminalwhereclause = "(assetgroup = ";

            foreach (string comb in lst_combination)
            {
                string[] ar_temp = comb.Split(',');
                str_terminalwhereclause += ar_temp[0];
                str_terminalwhereclause += " AND assettype = ";
                str_terminalwhereclause += ar_temp[1];
                str_terminalwhereclause += ") OR (assetgroup = ";
            }

            str_terminalwhereclause = str_terminalwhereclause.Substring(0, str_terminalwhereclause.LastIndexOf(')') + 1);
            return str_terminalwhereclause;
        }
        public static string GetDomainDesc(Domain domain, object num_domaincode)
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
                        if (item.Key.ToString().ToLower() == num_domaincode.ToString().ToLower())
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
        public static void Report_Sb_Sp_Trns_Connect_To_Line(StreamWriter sw,System.Windows.Controls.Label lblpro, string str_UniqueidField)
        {
            List<string> lst_reports = new List<string>();
            Geodatabase sourcegdb = ClassRequiredDetails.Geodatabase;
            try
            {
                sw.AutoFlush = true;
                FeatureClass fcl_Line = null;
                FeatureClass fcl_device = null;

                Table Domen_net_tab = sourcegdb.OpenDataset<Table>("A_DomainNetwork");
                string str_domainnetwork = GetDomainNetwork(Domen_net_tab);
                
                fcl_Line = sourcegdb.OpenDataset<FeatureClass>(str_domainnetwork + "Line");
                fcl_device = sourcegdb.OpenDataset<FeatureClass>(str_domainnetwork + "Device");
                Dictionary<int, string> dic_subtype_line = Get_subtype_code_val(fcl_Line);
                Dictionary<int, string> dic_subtype_dev =Get_subtype_code_val(fcl_device);
                if (!FieldExists(fcl_Line, fld_opt_voltage))
                {
                    return;
                }
                double XYtolerence = MainWindow.UNTolerance;
                // string transformer_config_name = "High Low Container";
                Table tab_terminal_ass = sourcegdb.OpenDataset<Table>("B_TerminalConfiguration_Assignment");
                lblpro.Content = ("Processing Step Transformer... ");
                System.Windows.Forms.Application.DoEvents();
                List<string> lst_asgp_astp_code_combination = get_asgp_astp_code_combination(tab_terminal_ass, fcl_device);

                string strclause = get_terminal_whereclause(lst_asgp_astp_code_combination);
                string str_subfield = "objectid,shape,assetgroup,assettype,globalid";
                var str_field_name = str_subfield.Split(',');

                if (!str_field_name.Contains(str_UniqueidField.ToLower()))
                {
                    str_subfield += "," + str_UniqueidField;
                }

                QueryFilter qf = new QueryFilter();
                qf.SubFields = str_subfield;
                qf.WhereClause = strclause;

                int num_crnt = 0;
                long num_cntmax = fcl_device.GetCount(qf);
                var fcur_device = fcl_device.Search(qf, false);
                while (fcur_device.MoveNext())
                {
                    num_crnt++;

                    lblpro.Content = ("Processing Step Transformer... " + num_crnt + "/" + num_cntmax);
                    System.Windows.Forms.Application.DoEvents();

                    Feature ft_device = fcur_device.Current as Feature;
                    List<string> lst_Device_details = get_feature_details(fcl_device, ft_device, dic_subtype_dev, str_UniqueidField);

                    SpatialQueryFilter sf = new SpatialQueryFilter();
                    sf.FilterGeometry = GeometryEngine.Instance.Buffer(ft_device.GetShape(), 0.0099);
                    sf.SpatialRelationship = SpatialRelationship.Intersects;
                    sf.SubFields = str_subfield + "," + fld_opt_voltage;
                    List<Feature> lst_connectfeature = new List<Feature>();
                    var linefcur = fcl_Line.Search(sf, false);
                    while (linefcur.MoveNext())
                    {
                        bool Is_mid_flag = false;
                        Feature ft_line = linefcur.Current as Feature;
                        Polyline poly = ft_line.GetShape() as Polyline;
                        ReadOnlyPointCollection pointlist = poly.Points;

                        for (int i = 0; i < pointlist.Count; i++)
                        {
                            if (GeometryEngine.Instance.Distance(ft_device.GetShape(), pointlist[i]) <= XYtolerence)
                            {
                                if ((ft_device.GetShape() as MapPoint).Z == pointlist[i].Z)
                                {
                                    //if ((GeometryEngine.Instance.Distance(ft_device.GetShape(), pointlist[0]) > 0.0099) && (GeometryEngine.Instance.Distance(ft_device.GetShape(), pointlist[pointlist.Count - 1]) > 0.0099))
                                    //{
                                    lst_connectfeature.Add(ft_line);
                                    Is_mid_flag = true;
                                    break;
                                    //}

                                }

                            }
                        }
                    }

                    if (lst_connectfeature.Count > 2)
                    {
                        string ag_combi = string.Empty;
                        foreach (var item in lst_connectfeature)
                        {
                            ag_combi += dic_subtype_line[Convert.ToInt32(item["Assetgroup"].ToString())] + " ";
                        }
                        string strrpt_combination = $"{fcl_device.GetName()},{lst_Device_details[0]},{lst_Device_details[1]},{lst_Device_details[2]},{lst_Device_details[3]},Connect To More Then Two Lines({ag_combi})";
                        lst_reports.Add(strrpt_combination);
                    }
                    else if (lst_connectfeature.Count == 2)
                    {
                        Feature ft_line1 = lst_connectfeature[0];
                        Feature ft_line2 = lst_connectfeature[1];
                        string strasgpline1 = dic_subtype_line[Convert.ToInt32(ft_line1["assetgroup"].ToString())];
                        string strasgpline2 = dic_subtype_line[Convert.ToInt32(ft_line2["assetgroup"].ToString())];
                        string strlinevoltage1 = ft_line1[fld_opt_voltage] == null || ft_line1[fld_opt_voltage] == DBNull.Value ? "Null" : ft_line1[fld_opt_voltage].ToString();
                        string strlinevoltage2 = ft_line2[fld_opt_voltage] == null || ft_line2[fld_opt_voltage] == DBNull.Value ? "Null" : ft_line2[fld_opt_voltage].ToString();


                        if (strlinevoltage1 == "Null" || strlinevoltage1 == "Null")
                        {
                            string strrpt_combination = $"{fcl_device.GetName()},{lst_Device_details[0]},{lst_Device_details[1]},{lst_Device_details[2]},{lst_Device_details[3]},Connected Lines Have Null Voltage";
                            lst_reports.Add(strrpt_combination);
                        }
                        else //if (strlinevoltage1.ToLower() == strlinevoltage2.ToLower())
                        {
                            //if (strlinevoltage1.ToLower() == strlinevoltage2.ToLower())
                            //{

                            //}
                            //string voltagedesc1 =
                            Domain domside1 = Cls_Common.GetDomainFromField(fcl_Line, fld_opt_voltage, dic_subtype_line[Convert.ToInt32(ft_line1["Assetgroup"].ToString())]);
                            string voltagedesc1 = GetDomainDesc(domside1, ft_line1[fld_opt_voltage]);

                            Domain domside2 = Cls_Common.GetDomainFromField(fcl_Line, fld_opt_voltage, dic_subtype_line[Convert.ToInt32(ft_line2["Assetgroup"].ToString())]);
                            string voltagedesc2 = GetDomainDesc(domside2, ft_line2[fld_opt_voltage]);
                            if (voltagedesc1.ToLower() == voltagedesc2.ToLower())
                            {
                                string strrpt_combination = $"{fcl_device.GetName()},{lst_Device_details[0]},{lst_Device_details[1]},{lst_Device_details[2]},{lst_Device_details[3]},Connected Lines Have Matching Voltage";
                                lst_reports.Add(strrpt_combination);
                            }

                        }


                    }
                }




            }
            catch (Exception e)
            {

            }
            finally
            {
                foreach (var item in lst_reports)
                {
                    sw.WriteLine(item);
                }
                //sw.WriteLine("EndTime : " + System.DateTime.Now.ToString("yyyyMMdd") + "_" + System.DateTime.Now.ToString("HHmmss"));
                sw.Dispose();
                sw.Close();
                lblpro.Content = ("Process Completed!");
            }



        }
        
        public static List<string> get_asgp_astp_code_combination(ArcGIS.Core.Data.Table B_TerminalConfiguration_Assignment, FeatureClass Fc_Devclass)
        {
            List<string> lst_asgp_astp_combi = new List<string>();
            var cursor = B_TerminalConfiguration_Assignment.Search(null, false);
            while (cursor.MoveNext())
            {
                try
                {
                    Row brow = cursor.Current;
                    string str_assgrp = brow[brow.FindField("asset_group")].ToString();
                    string str_asstyp = brow[brow.FindField("asset_type")].ToString();
                    string str_configname = brow[brow.FindField("terminal_configuration_name")].ToString();
                    string Fcname = brow[brow.FindField("feature_class")].ToString();
                    if (Fcname.ToLower().Contains("device"))
                    {
                        string combination = str_assgrp + "_" + str_asstyp;
                        if (combination.ToLower().Contains("transformer"))
                        {
                            if (str_assgrp.ToLower().Contains("step") || str_assgrp.ToLower().Contains("substation") || str_asstyp.ToLower().Contains("step") ||
                                str_asstyp.ToLower().Contains("substation") || str_assgrp.ToLower().Contains("network") || str_asstyp.ToLower().Contains("network"))
                            {
                                Domain dom = Cls_Common.GetDomainFromField(Fc_Devclass, "assettype", str_assgrp);
                                int asstypecode = Cls_Common.GetDomainCode(dom, str_asstyp);
                                int assgrpcode = (Fc_Devclass.GetDefinition().GetSubtypes().First(x => x.GetName().ToLower() == str_assgrp.ToLower())).GetCode();

                                lst_asgp_astp_combi.Add(assgrpcode + "," + asstypecode);
                            }
                        }

                    }


                }
                catch (Exception ex)
                {
                }

            }
            return lst_asgp_astp_combi;
        }
    }
}
