using ArcGIS.Core.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace My_Design.Tools.Modification
{
    class Create_Containment_Between_Device_Ejo : IPostProcessTool
    {
        private ToolInfo toolInfo = null;
        public Create_Containment_Between_Device_Ejo(ToolInfo _toolInfo)
        {
            toolInfo = _toolInfo;
        }
        public void Process(System.Windows.Controls.Label lblpro, string uniqueid)
        {
            Containment_Device_JunctionObjects(lblpro);
        }
        public static void Containment_Device_JunctionObjects(System.Windows.Controls.Label LblPro)
        {
            List<string> lst_not_containment_report = new List<string>();
            List<string> lst_phase_mismatch_report = new List<string>();

            string ReportPath = UserControl1.reportpath;
            Geodatabase sourcegdb = ClassRequiredDetails.Geodatabase;
            string domainstring = "Electric";
            DateTime currentTime = DateTime.Now;
            string timestamp = currentTime.ToString("yyyy-MM-dd_HH-mm-ss");

            string rptName = $"Containment_Device_Junction_Object_{timestamp}.csv";
            StreamWriter sw = new StreamWriter(Path.Combine(UserControl1.reportpath, rptName));
            sw.WriteLine("Featureclass1,Assetgroup1,Assettype1,Globalid1,Featureclass2,Assetgroup2,Assettype2,Globalid2,Remarks");

            string rptName_1 = $"Device_Ejo_Child_Missing_{timestamp}.csv";
            StreamWriter sw_1 = new StreamWriter(Path.Combine(UserControl1.reportpath, rptName_1));
            sw_1.WriteLine("Featureclass1,Assetgroup1,Assettype1,Globalid1,SourceOid1,Remarks");

            string rptName_2 = $"Device_Ejo_Phase_Mismatch_{timestamp}.csv";
            StreamWriter sw_2 = new StreamWriter(Path.Combine(UserControl1.reportpath, rptName_2));
            sw_2.WriteLine("Featureclass1,Assetgroup1,Assettype1,Globalid1,SourceOid1,Remarks");

            sw.AutoFlush = true;
            sw_1.AutoFlush = true;
            sw_2.AutoFlush = true;

            List<string> Object_dev_list = new List<string>();
            Table fcljunObject = sourcegdb.OpenDataset<Table>(domainstring + "JunctionObject");
            FeatureClass fcldevice = sourcegdb.OpenDataset<FeatureClass>(domainstring + "Device");
            Table tbl_casso = sourcegdb.OpenDataset<Table>("C_Associations");
            TableDefinition tbldefobject = fcljunObject.GetDefinition();
            TableDefinition tbldefdev = fcldevice.GetDefinition();
            LblPro.Content = "Processing....";
            System.Windows.Forms.Application.DoEvents();
            Table domnettab = sourcegdb.OpenDataset<Table>("A_DomainNetwork");
            int domain_Network_code = ClassRequiredDetails.GetDomainNetworkCode(domnettab);
            Dictionary<string, List<string>> dic_junobject = getDic_juncObject(fcljunObject, domain_Network_code, sw, LblPro);
            int cnt = 0;


            //QueryFilter qf = new QueryFilter
            //{
            //    WhereClause = "ASSETGROUP = 217"
            //};
            long cntmax = fcldevice.GetCount();
            var fcur_dev = fcldevice.Search(null, true);

            while (fcur_dev.MoveNext())
            {
                cnt++;
                LblPro.Content = "Processing ...." + cnt + "/" + cntmax;
                System.Windows.Forms.Application.DoEvents();

                List<string> List_xx = null;

                Row ftobj = fcur_dev.Current;
                string agcode_object = ftobj["assetgroup"].ToString();
                Subtype stypeobject = tbldefdev.GetSubtypes().First(x => x.GetCode() == Convert.ToInt32(agcode_object));
                string asgroup_object = stypeobject.GetName();
                Domain dom_object_asstype = ClassRequiredDetails.GetDomainFromField(fcldevice, "assettype", asgroup_object);
                string atcode_object = ftobj["assettype"].ToString();
                string at_object = ClassRequiredDetails.GetDomainDesc(dom_object_asstype, Convert.ToInt32(atcode_object));
                string gid_object = ftobj["globalid"].ToString();
                object legacyid = ftobj["source_oid"];
                string sourceFC = ftobj["sourcefeatureclass"].ToString();

                if (sourceFC.ToLower().Contains("openpoint"))
                {
                    continue;
                }
                if (sourceFC.ToLower().Contains("servicepoint"))
                {
                    continue;
                }
                if (sourceFC.ToLower().Contains("streetlight"))
                {
                    continue;
                }
                if (sourceFC.ToLower().Contains("generator"))
                {
                    continue;
                }
                if (sourceFC.ToLower().Contains("miscnetworkfeature"))
                {
                    continue;
                }

                if (asgroup_object.ToLower().Contains("breaker")) continue;

                if (legacyid == null || sourceFC == null) continue;

                var DicSubtype = ClassRequiredDetails.Get_subtype_code_val(fcldevice);
                var ag = ftobj["Assetgroup"];
                string AGdesc = DicSubtype[Convert.ToInt32(ag.ToString())];
                Domain phasedom = ClassRequiredDetails.GetDomainFromField(fcldevice, "Phasesnormal", AGdesc);

                Domain dom_phase2domain = GetDomainFromField(fcldevice, "phasesnormal",AGdesc );
                string parentPhase = ClassRequiredDetails.GetDomainDesc(dom_phase2domain, Convert.ToInt32(ftobj["phasesnormal"]));
               

                bool Is_containment_created = false;
                if (sourceFC == "PFCorrectingEquipment")
                {
                    string match1 = legacyid + "_" + "PFCorrectingEquipment";
                    string match2 = legacyid + "_" + "CAPACITORUNIT";
                    string match3 = legacyid + "_" + "CapacitorUnit";
                    bool iS_Case_match = false;
                    if (dic_junobject.ContainsKey(match1))
                    {
                        List_xx = dic_junobject[match1];
                        Is_containment_created = true;
                        iS_Case_match = true;
                    }
                    else if (dic_junobject.ContainsKey(match2))
                    {
                        List_xx = dic_junobject[match2];
                        Is_containment_created = true;
                        iS_Case_match = true;
                    }
                    else if (dic_junobject.ContainsKey(match3))
                    {
                        List_xx = dic_junobject[match3];
                        Is_containment_created = true;
                        iS_Case_match = true;
                    }
                    if (!iS_Case_match)
                    {
                        lst_not_containment_report.Add("Device" + "," + asgroup_object + "," + at_object + "," + gid_object + "," + legacyid + "," + "Sourcefc Not Matched");
                        continue;
                    }

                    

                }
                else if (sourceFC == "DynamicProtectiveDevice")
                {
                    string match5 = legacyid + "_" + "DynamicProtectiveDevice";
                    string match6 = legacyid + "_" + "RECLOSERUNIT";
                    string match7 = legacyid + "_" + "SECTIONALIZERUNIT";
                    string match8 = legacyid + "_" + "RecloserUnit";
                    string match9 = legacyid + "_" + "SectionalizerUnit";
                    bool iS_Case_match = false;

                    if (dic_junobject.ContainsKey(match5))
                    {
                        List_xx = dic_junobject[match5];
                        Is_containment_created = true;
                        iS_Case_match = true;

                    }
                    else if (dic_junobject.ContainsKey(match6))
                    {
                        List_xx = dic_junobject[match6];
                        Is_containment_created = true;
                        iS_Case_match = true;
                    }
                    else if (dic_junobject.ContainsKey(match7))
                    {
                        List_xx = dic_junobject[match7];
                        Is_containment_created = true;
                        iS_Case_match = true;
                    }
                    else if (dic_junobject.ContainsKey(match8))
                    {
                        List_xx = dic_junobject[match8];
                        Is_containment_created = true;
                        iS_Case_match = true;
                    }
                    else if (dic_junobject.ContainsKey(match9))
                    {
                        List_xx = dic_junobject[match9];
                        Is_containment_created = true;
                        iS_Case_match = true;
                    }

                    if (!iS_Case_match)
                    {
                        lst_not_containment_report.Add("Device" + "," + asgroup_object + "," + at_object + "," + gid_object + "," + legacyid + "," + "Sourcefc Not Matched");
                        continue;
                    }
                   
                }
                else if (dic_junobject.ContainsKey(legacyid + "_" + sourceFC.Trim()) || dic_junobject.ContainsKey(legacyid + "_" + sourceFC.Trim().ToUpper() + "UNIT") || dic_junobject.ContainsKey(legacyid + "_" + sourceFC.Trim().ToUpper() + "Unit") || dic_junobject.ContainsKey(legacyid + "_" + sourceFC.Trim() + "Unit"))
                {
                    Is_containment_created = true;
                    string match1 = legacyid + "_" + sourceFC.Trim();
                    string match2 = legacyid + "_" + sourceFC.Trim().ToUpper() + "UNIT";
                    string match3 = legacyid + "_" + sourceFC.Trim().ToUpper() + "Unit";
                    string match4 = legacyid + "_" + sourceFC.Trim() + "Unit";
                    bool iS_Case_match = false;
                    if (dic_junobject.ContainsKey(match1))
                    {
                        List_xx = dic_junobject[match1];
                        iS_Case_match = true;
                    }
                    else if (dic_junobject.ContainsKey(match2))
                    {
                        List_xx = dic_junobject[match2];
                        iS_Case_match = true;
                    }
                    else if (dic_junobject.ContainsKey(match3))
                    {
                        List_xx = dic_junobject[match3];
                        iS_Case_match = true;
                    }
                    else if (dic_junobject.ContainsKey(match4))
                    {
                        List_xx = dic_junobject[match4];
                        iS_Case_match = true;
                    }
                    if (!iS_Case_match)
                    {
                        lst_not_containment_report.Add("Device" + "," + asgroup_object + "," + at_object + "," + gid_object + "," + legacyid + "," + "Sourcefc Not Matched");
                        continue;
                    }

                    
                }
                else
                {

                    lst_not_containment_report.Add("Device" + "," + asgroup_object + "," + at_object + "," + gid_object + "," + "Containment Not created as sourcefc not matched");
                    continue;
                }

                if (List_xx != null)
                {
                    Dictionary<string, string> uniqueChildMap = new Dictionary<string, string>();
                    HashSet<char> usedPhases = new HashSet<char>();
                    bool hasPhaseError = false;

                    foreach (string xx in List_xx)
                    {
                        var tokens = xx.Split(',');
                        string childGlobalId = tokens[4];
                        string childPhase = tokens[5].ToUpper().Trim();
                        string childAssetGroup = tokens[2];
                        string childAssetType = tokens[3];

                        bool isValidChild = true;

                        foreach (char c in childPhase)
                        {
                            if (!parentPhase.Contains(c))
                            {
                                lst_not_containment_report.Add($"Device,{asgroup_object},{at_object},{gid_object},JunctionObject,{childAssetGroup},{childAssetType},{childGlobalId},InvalidPhase Parent={parentPhase} Child={childPhase}");
                                isValidChild = false;
                                break;
                            }

                            if (usedPhases.Contains(c))
                            {
                                lst_not_containment_report.Add($"Device,{asgroup_object},{at_object},{gid_object},JunctionObject,{childAssetGroup},{childAssetType},{childGlobalId},More Unit records of same Phase={c}");
                                isValidChild = false;
                                break;
                            }
                        }

                        if (!isValidChild) continue;

                        foreach (char c in childPhase)
                        {
                            usedPhases.Add(c);
                        }

                        if (!uniqueChildMap.ContainsKey(childGlobalId))
                            uniqueChildMap[childGlobalId] = xx;
                    }

                    if (!hasPhaseError)
                    {
                        foreach (var kvp in uniqueChildMap)
                        {
                            string xx = kvp.Value;
                            string yy = "Containment" + "," + domain_Network_code + "," + "Device" + "," + asgroup_object + "," + at_object + "," + gid_object + "," + xx;
                            Object_dev_list.Add(yy);
                        }

                        if (usedPhases.Count < parentPhase.Distinct().Count())
                        {
                            lst_not_containment_report.Add($"Device,{asgroup_object},{at_object},{gid_object},Containment created but child record count mismatch as per parent phase ParentPhase={parentPhase} UsedPhases={usedPhases.Count}");
                        }

                        Is_containment_created = true;
                    }
                }

                if (!Is_containment_created)
                {
                    lst_not_containment_report.Add("Device" + "," + asgroup_object + "," + at_object + "," + gid_object + "," + legacyid + "," + "Containment Not created");
                }
            }

            int cntx = 0;
            int cntxmax = Object_dev_list.Count;
            foreach (string str in Object_dev_list)
            {
                cntx++;
                LblPro.Content = "Inserting row ...containment..." + cntx + "/" + cntxmax;
                System.Windows.Forms.Application.DoEvents();
                using (RowBuffer row = tbl_casso.CreateRowBuffer())
                {
                    var tokens = str.Split(',');
                    row["association_type"] = "Containment";
                    row["from_domain_network"] = tokens[1];
                    row["from_feature_class"] = tokens[2];
                    row["from_asset_group"] = tokens[3];
                    row["from_asset_type"] = tokens[4];
                    row["from_global_id"] = tokens[5];
                    row["to_domain_network"] = tokens[6];
                    row["to_feature_class"] = tokens[7];
                    row["to_asset_group"] = tokens[8];
                    row["to_asset_type"] = tokens[9];
                    row["to_global_id"] = tokens[10];
                    row["content_visible"] = 1;
                    using (Row roww = tbl_casso.CreateRow(row))
                    {
                        sw.WriteLine(tokens[2] + "," + tokens[3] + "," + tokens[4] + "," + tokens[5] + "," + tokens[7] + "," + tokens[8] + "," + tokens[9] + "," + tokens[10] + "," + "Containment Created");
                    }
                }
            }

            foreach (var item in lst_not_containment_report)
            {
                sw_1.WriteLine(item);
            }
            foreach (var item in lst_phase_mismatch_report)
            {
                sw_2.WriteLine(item);
            }

            sw.Close();
            sw.Dispose();

            sw_1.Close();
            sw_1.Dispose();

            sw_2.Close();
            sw_2.Dispose();

            LblPro.Content = "Process completed";
        }

        public static Dictionary<string, List<string>> getDic_juncObject(Table junobjtable, object domainNetwork_code, StreamWriter sw, System.Windows.Controls.Label LblPro)
        {
            TableDefinition tbldefJunobj = junobjtable.GetDefinition();
            Dictionary<string, List<string>> dic_xx = new Dictionary<string, List<string>>();
            int cnt = 0;
            long cntmax = junobjtable.GetCount();
            var curjunobj = junobjtable.Search(null, true);
            while (curjunobj.MoveNext())
            {
                cnt++;
                LblPro.Content = "Reading JunctionObject...." + cnt + "/" + cntmax;
                System.Windows.Forms.Application.DoEvents();
                Row row = curjunobj.Current;
                string agcode_obj = row["assetgroup"].ToString();
                Subtype stypeobject = tbldefJunobj.GetSubtypes().First(x => x.GetCode() == Convert.ToInt32(agcode_obj));
                string atcode_obj = row["assettype"].ToString();
                string asgroup_obj = stypeobject.GetName();
                Domain dom_obj_asstype = ClassRequiredDetails.GetDomainFromField(junobjtable, "assettype", asgroup_obj);
                string asttype_obj = ClassRequiredDetails.GetDomainDesc(dom_obj_asstype, Convert.ToInt32(atcode_obj));
                string gid_obj = row["globalid"].ToString();
                string source_oid = row["legacyparentoid"] == null || row["legacyparentoid"] == DBNull.Value ? "Null" : row["legacyparentoid"].ToString();
                //string parentFC = row["sourcefeatureclass"].ToString();
                string parentFC = row["sourcefeatureclass"] == null || row["sourcefeatureclass"] == DBNull.Value ? "Null" : row["sourcefeatureclass"].ToString();

                int PhaseCode = Convert.ToInt32(row["Phasesnormal"].ToString());
                Domain phasedom = GetDomainFromField(junobjtable, "Phasesnormal", asgroup_obj);
                string childPhase = GetDomainDesc(phasedom, PhaseCode);


                string xx = domainNetwork_code + "," + "JunctionObject" + "," + asgroup_obj + "," + asttype_obj + "," + gid_obj + "," + childPhase;

                if (!dic_xx.ContainsKey(source_oid + "_" + parentFC))
                {
                    dic_xx.Add(source_oid + "_" + parentFC, new List<string>() { xx });
                }
                else
                {
                    dic_xx[source_oid + "_" + parentFC].Add(xx);
                }
            }
            return dic_xx;
        }
        public static Domain GetDomainFromField(Table Fc_req_class, string fieldname, string subtypedesc)
        {
            ArcGIS.Core.Data.Domain domain = null;
            IReadOnlyList<Subtype> subtypelist_dev = Fc_req_class.GetDefinition().GetSubtypes();
            TableDefinition tableDefinition = Fc_req_class.GetDefinition();
            Subtype subtype = tableDefinition.GetSubtypes().First(x => x.GetName().ToLower() == subtypedesc.ToLower());

            Field field = tableDefinition.GetFields().FirstOrDefault(f => f.Name.ToLower().Equals(fieldname.ToLower(), StringComparison.OrdinalIgnoreCase));

            if (field != null && field.GetDomain(subtype) is CodedValueDomain codedValueDomain)
            {
                domain = codedValueDomain;
            }

            return domain;

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
    }
}
