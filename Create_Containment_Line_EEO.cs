using ArcGIS.Core.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace My_Design.Tools.Modification
{
    class Create_Containment_Between_Line_Eeo : IPostProcessTool
    {
        private ToolInfo toolInfo = null;
        public Create_Containment_Between_Line_Eeo(ToolInfo _toolInfo)
        {
            toolInfo = toolInfo;
        }
        public void Process(System.Windows.Controls.Label lblpro, string uniqueid)
        {
            Containment_Line_EdgeObjects(lblpro, uniqueid);
        }
        public static void Containment_Line_EdgeObjects(System.Windows.Controls.Label LblPro, string uniqueid)
        {
            List<string> lst_not_containment_report = new List<string>();
            string ReportPath = UserControl1.reportpath;
            Geodatabase sourcegdb = ClassRequiredDetails.Geodatabase;
            string domainstring = "Electric";
            DateTime currentTime = DateTime.Now;
            string timestamp = currentTime.ToString("yyyy-MM-dd_HH-mm-ss");

            string rptName = $"{"Containment_Line_EdgeObject"}_{timestamp}.csv";
            StreamWriter sw = new StreamWriter(Path.Combine(UserControl1.reportpath, rptName));
            sw.WriteLine("Featureclass1,Assetgroup1,Assettype1,Globalid1,Featureclass2,Assetgroup2,Assettype2,Globalid2,Remarks");
            sw.AutoFlush = true;

            string rptName_1 = $"{"Containment_Line_EdgeObject_Not_Created"}_{timestamp}.csv";
            StreamWriter sw_2 = new StreamWriter(Path.Combine(UserControl1.reportpath, rptName_1));
            sw_2.WriteLine("Featureclass1,Assetgroup1,Assettype1,Globalid1,Featureclass2,Assetgroup2,Assettype2,Globalid2,Remarks");
            sw_2.AutoFlush = true;

            List<string> Object_dev_list = new List<string>();
            Dictionary<long, List<long>> dic_devObject = new Dictionary<long, List<long>>();
            List<string> List_xx = null;
            //Table domnettab = null;
            Table fcledgeObject = null;
            FeatureClass fclLine = null;
            Table tbl_casso = null;
            Table domnettab = sourcegdb.OpenDataset<Table>("A_DomainNetwork");
            int domain_Network_code = ClassRequiredDetails.GetDomainNetworkCode(domnettab);
            fcledgeObject = sourcegdb.OpenDataset<Table>(domainstring + "EdgeObject");
            fclLine = sourcegdb.OpenDataset<FeatureClass>(domainstring + "Line");
            tbl_casso = sourcegdb.OpenDataset<Table>("C_Associations");
            TableDefinition tbldefobject = fcledgeObject.GetDefinition();
            TableDefinition tbldefline = fclLine.GetDefinition();
            //TableDefinition tbldefjun = fcljunction.GetDefinition();
            LblPro.Content = "Processing....";
            System.Windows.Forms.Application.DoEvents();
            Dictionary<string, List<string>> dic_edgeobject = getDic_edgeObject(fcledgeObject, domain_Network_code, sw, LblPro);

            //QueryFilter qfilter = new QueryFilter();
            //qfilter.WhereClause = "Assetgroup = " + 2 + " and RT_Source_FC = 'MeterPoint'" + " and rt_source_oid is not null";
            QueryFilter qf = new QueryFilter();
            int cnt = 0;
            long cntmax = fclLine.GetCount(null);
            var fcur_line = fclLine.Search(null, true);
            while (fcur_line.MoveNext())
            {
                cnt++;
                LblPro.Content = "Processing ...." + cnt + "/" + cntmax;
                System.Windows.Forms.Application.DoEvents();
                Row ftobj = fcur_line.Current;
                //Feature ftjun = getJun(ftobj, fcljunction);
                string agcode_object = ftobj["assetgroup"].ToString();
                Subtype stypeobject = tbldefline.GetSubtypes().First(x => x.GetCode() == Convert.ToInt32(agcode_object));
                string asgroup_object = stypeobject.GetName();
                Domain dom_object_asstype = GetDomainFromField(fclLine, "assettype", asgroup_object);
                string atcode_object = ftobj["assettype"].ToString();
                string at_object = GetDomainDesc(dom_object_asstype, Convert.ToInt32(atcode_object));
                string gid_object = ftobj["globalid"].ToString();
                object legacyid = ftobj["source_oid"];
                object sourceFC = ftobj["sourcefeatureclass"];
                if (legacyid == null || sourceFC == null) continue;

                var DicSubtype = ClassRequiredDetails.Get_subtype_code_val(fclLine);
                var ag = ftobj["Assetgroup"];
                string AGdesc = DicSubtype[Convert.ToInt32(ag.ToString())];
                Domain phasedom = GetDomainFromField(fclLine, "Phasesnormal", AGdesc);
                var ph1 = Convert.ToInt32(ftobj["Phasesnormal"].ToString());
                string parentPhase = GetDomainDesc(phasedom, ph1).ToUpper().Trim();
                bool Is_containment_created = false;
                if (dic_edgeobject.ContainsKey(legacyid + "_" + sourceFC))
                {
                    List_xx = dic_edgeobject[legacyid + "_" + sourceFC];

                }
                else
                {
                    lst_not_containment_report.Add(domainstring + "Line" + "," + asgroup_object + "," + at_object + "," + gid_object + "," + "Containment Not created");
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
                                lst_not_containment_report.Add($"Line,{asgroup_object},{at_object},{gid_object},EdgeObject,{childAssetGroup},{childAssetType},{childGlobalId},InvalidPhase Parent={parentPhase} Child={childPhase}");
                                isValidChild = false;
                                break;
                            }

                            if (usedPhases.Contains(c))
                            {
                                lst_not_containment_report.Add($"Line,{asgroup_object},{at_object},{gid_object},EdgeObject,{childAssetGroup},{childAssetType},{childGlobalId},More Unit records of same Phase={c}");
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
                            string yy = "Containment" + "," + domain_Network_code + "," + "Line" + "," + asgroup_object + "," + at_object + "," + gid_object + "," + xx;
                            Object_dev_list.Add(yy);
                        }

                        if (usedPhases.Count < parentPhase.Distinct().Count())
                        {
                            lst_not_containment_report.Add($"Line,{asgroup_object},{at_object},{gid_object},Containment created but child record count mismatch as per parent phase ParentPhase={parentPhase} UsedPhases={usedPhases.Count}");
                        }
                        Is_containment_created = true;

                    }
                }
                if (!Is_containment_created)
                {
                    lst_not_containment_report.Add("Line" + "," + asgroup_object + "," + at_object + "," + gid_object + "," + "Containment Not created");
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
                    row["association_type"] = "Containment";
                    row["from_domain_network"] = str.Split(',')[1];
                    row["from_feature_class"] = str.Split(',')[2];
                    row["from_asset_group"] = str.Split(',')[3];
                    row["from_asset_type"] = str.Split(',')[4];
                    row["from_global_id"] = str.Split(',')[5];
                    row["to_domain_network"] = str.Split(',')[6];
                    row["to_feature_class"] = str.Split(',')[7];
                    row["to_asset_group"] = str.Split(',')[8];
                    row["to_asset_type"] = str.Split(',')[9];
                    row["to_global_id"] = str.Split(',')[10];
                    row["content_visible"] = 1;
                    using (Row roww = tbl_casso.CreateRow(row))
                    {
                        //roww.Store();
                        sw.WriteLine(str.Split(',')[2] + "," + str.Split(',')[3] + "," + str.Split(',')[4] + "," + str.Split(',')[5] + "," + str.Split(',')[7] + "," + str.Split(',')[8] + "," + str.Split(',')[9] + "," + str.Split(',')[10] + "," + "Containment Created");
                    }
                }
            }
            foreach (var item in lst_not_containment_report)
            {
                sw_2.WriteLine(item);
            }
            sw.Close();
            sw.Dispose();

            sw_2.Close();
            sw_2.Dispose();
            LblPro.Content = "Process completed";
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
        public static Dictionary<string, List<string>> getDic_edgeObject(Table edgeobjtable, object domainNetwork_code, StreamWriter sw, System.Windows.Controls.Label LblPro)
        {
            TableDefinition tbldefJunobj = edgeobjtable.GetDefinition();
            Dictionary<string, List<string>> dic_xx = new Dictionary<string, List<string>>();
            QueryFilter queryFilter = new QueryFilter();
            queryFilter.WhereClause = $"sourcefeatureclass <> 'PriOHElectricLineSegment'";
            int cnt = 0;
            long cntmax = edgeobjtable.GetCount(queryFilter);
            var curjunobj = edgeobjtable.Search(queryFilter, true);
            while (curjunobj.MoveNext())
            {
                cnt++;
                LblPro.Content = "Reading EdgeObject...." + cnt + "/" + cntmax;
                System.Windows.Forms.Application.DoEvents();
                Row row = curjunobj.Current;
                string agcode_obj = row["assetgroup"].ToString();
                Subtype stypeobject = tbldefJunobj.GetSubtypes().First(x => x.GetCode() == Convert.ToInt32(agcode_obj));
                string atcode_obj = row["assettype"].ToString();
                string asgroup_obj = stypeobject.GetName();
                Domain dom_obj_asstype = ClassRequiredDetails.GetDomainFromField(edgeobjtable, "assettype", asgroup_obj);
                string asttype_obj = ClassRequiredDetails.GetDomainDesc(dom_obj_asstype, Convert.ToInt32(atcode_obj));
                string gid_obj = row["globalid"].ToString();

                int PhaseCode = Convert.ToInt32(row["Phasesnormal"].ToString());
                Domain phasedom = GetDomainFromField(edgeobjtable, "Phasesnormal", asgroup_obj);
                string childPhase = GetDomainDesc(phasedom, PhaseCode);

                string relatedid = row["legacyconductorobjectid"] == null || row["legacyconductorobjectid"].ToString() == "0" ? "Null" : row["legacyconductorobjectid"].ToString();
               
                string source = row["sourcefeatureclass"].ToString();
                string source_fc = null;
                if (source == "PriOHConductorInfo")
                {
                    source_fc = "PriOHElectricLineSegment";
                }
                else if (source == "PriUGConductorInfo")
                {
                    source_fc = "PriUGElectricLineSegment";
                }
                else if (source == "SecOHConductorInfo")
                {
                    source_fc = "SecOHElectricLineSegment";
                }
                else if (source == "SecUGConductorInfo")
                {
                    source_fc = "SecUGElectricLineSegment";
                }

                string xx = domainNetwork_code + "," + "EdgeObject" + "," + asgroup_obj + "," + asttype_obj + "," + gid_obj + "," + childPhase;

                if (relatedid == null || source_fc == null) continue;

                if (!dic_xx.ContainsKey(relatedid + "_" + source_fc))
                {
                    dic_xx.Add(relatedid + "_" + source_fc, new List<string>() { xx });
                }
                else
                {
                    //sw.WriteLine(",,,,,,,," + "Multiple Record" + "," + legacyid);
                    dic_xx[relatedid + "_" + source_fc].Add(xx);
                }

            }
            return dic_xx;
        }

    }
}
