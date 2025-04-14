    //int l_assgrp_1 = Convert.ToInt32(line_1["assetgroup"]);
    //int l_asstyp_1 = Convert.ToInt32(line_1["assettype"]);
    //string l_asstypdesc_1 = GetAssetTypeDescription(LineClass, l_assgrp_1, l_asstyp_1, out string l_assgrpdesc_1);   



     public static string GetAssetTypeDescription(FeatureClass featureClass, int assetGroup, int assetType, out string assetGroupDesc)
        {
            string assetTypeDesc = "";
            assetGroupDesc = "";
            var subtypes = featureClass.GetDefinition().GetSubtypes();
            ArcGIS.Core.Data.Subtype matchingSubtype = null;
            foreach (var subtype in subtypes)
            {
                if (subtype.GetCode() == assetGroup)
                {
                    matchingSubtype = subtype;
                    ArcGIS.Core.Data.Field assetTypeField = featureClass.GetDefinition().GetFields().FirstOrDefault(field => field.Name.ToUpper() == "ASSETTYPE");
                    var Astype_Domain = assetTypeField.GetDomain(subtype) as ArcGIS.Core.Data.CodedValueDomain;
                    if (Astype_Domain != null)
                    {
                        foreach (var type_code_Val in Astype_Domain.GetCodedValuePairs())
                        {
                            if (Convert.ToInt32(type_code_Val.Key) == assetType)
                            {
                                assetTypeDesc = type_code_Val.Value;
                                break;
                            }
                        }
                    }
                    assetGroupDesc = subtype.GetName();
                    break;
                }
            }
            return assetTypeDesc;
        }
