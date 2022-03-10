// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{

    internal class PostProcessAuditProperties : PostProcess
    {
        private int maximumFileSizeInAcceptedRange = -1;  //File size can't be negative
        AuditFileSizeUnit maximumFileSizeUnit;        

        PostProcessAuditProperties()
        {
        }

        protected override bool SupportDataReader
        {
            get { return false; }
        }

        public override object GetColumnData(string name, object data, DataProvider dp)
        {
            switch (name)
            {
                case "MaximumFileSize":
                    data = GetMaximumFileSize(dp);
                    break;
                case "MaximumFileSizeUnit":
                    data = GetMaximumFileSizeUnit(dp);
                    break;
            }
            return data;
        }

        /// <summary>
        /// Return the MaximumFileSize value in the range 0 to 2^31-1
        /// </summary>
        private int GetMaximumFileSize(DataProvider dp)
        {
            if (maximumFileSizeInAcceptedRange < 0) //this will be negative till the getter of this property or MaximumFileSizeUnit property will be called for the first time
            {
                GetMaxFileSizeValueInAcceptedRangeAndUnit(dp);
            }

            return maximumFileSizeInAcceptedRange;
        }

        /// <summary>
        /// Gets and Sets the unit of MaximumFileSize in MB/GB/TB; 0=MB, 1=GB, 2=TB
        /// </summary>        
        private AuditFileSizeUnit GetMaximumFileSizeUnit(DataProvider dp)
        {
            if (maximumFileSizeInAcceptedRange < 0)  //this will be negative till the getter of this property or MaximumFileSize property will be called for the first time
            {
                GetMaxFileSizeValueInAcceptedRangeAndUnit(dp);
            }

            return maximumFileSizeUnit;
        }

        /// <summary>
        /// Gets MaximumFileSize in MB
        /// </summary>        
        private long GetMaximumFileSizeInMegaBytes(DataProvider dp)
        {
            if (GetTriggeredObject(dp, 0) != null)
            {
                return (long)GetTriggeredObject(dp, 0);
            }
            else
            {
                return (long)(-1);
            }
        }

        /// <summary>
        /// Gets Maximum File Size Value In Accepted Format and its unit from The Catalog value of maximum file size in Mega Bytes.
        /// </summary>
        /// <param name="dp"></param>
        private void GetMaxFileSizeValueInAcceptedRangeAndUnit(DataProvider dp)
        {
            long tempMaxFileSize = 0;        //default
            this.maximumFileSizeUnit = AuditFileSizeUnit.Mb;     //default
            tempMaxFileSize = GetMaximumFileSizeInMegaBytes(dp);
            this.maximumFileSizeUnit = ConvertFileSizeToAcceptedFormat1(ref tempMaxFileSize); // by using this function, we can evaluate both maxFileSize and 
                                                                                //maxFileSizeUnit without any overhead, so assigning both.
            this.maximumFileSizeInAcceptedRange = (int)tempMaxFileSize;
        }

        private AuditFileSizeUnit ConvertFileSizeToAcceptedFormat1(ref long maxFileSize)
        {
            long tempMaxFileSize = maxFileSize;

            if (maxFileSize > int.MaxValue)
            {
                double tempSize = tempMaxFileSize / 1024; //1 GB = 2^10 MB = 1024 MB
                maxFileSize = (long)tempSize;
                if (maxFileSize < tempSize)
                {
                    maxFileSize += 1;
                }

                if (maxFileSize > int.MaxValue)
                {
                    tempSize = tempMaxFileSize / 1048576;  //1TB = 2^20 MB = 1048576 MB
                    maxFileSize = (int)tempSize;
                    if (maxFileSize < tempSize)
                    {
                        maxFileSize += 1;
                    }

                    if (maxFileSize > int.MaxValue)
                    {
                        maxFileSize = 0; //Unlimited
                        return AuditFileSizeUnit.Mb;
                    }

                    return AuditFileSizeUnit.Tb;
                }

                return AuditFileSizeUnit.Gb;
            }
            else
            {
                return AuditFileSizeUnit.Mb;
            }
        }   
    }
}