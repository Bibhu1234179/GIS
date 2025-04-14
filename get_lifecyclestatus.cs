//int LifecycleStatus = Convert.ToInt32(feature["lifecyclestatus"]);
//string lifecycle = ClassRequiredDetails.GetLifecyclestatus(fc, "lifecyclestatus", int.Parse(LifecycleStatus.ToString())); 

       public static string GetLifecyclestatus(FeatureClass FC, string field_name, int lifecyclecode)
        {
            string lifecycle = "";

            try
            {
                ArcGIS.Core.Data.Field field = FC.GetDefinition().GetFields().FirstOrDefault(field => field.Name.ToUpper() == field_name.ToUpper());
                if (field != null)
                {
                    var domain = field.GetDomain();
                    if (domain is ArcGIS.Core.Data.CodedValueDomain codedValueDomain)
                    {
                        foreach (var codedValue in codedValueDomain.GetCodedValuePairs())
                        {
                            if (Convert.ToInt32(codedValue.Key) == lifecyclecode)
                            {
                                lifecycle = codedValue.Value;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return lifecycle;
        }