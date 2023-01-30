// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Collections;
    using Microsoft.SqlServer.Management.Common;

    /// <summary>
    /// core of enumerator: implements GetData and GetObjectInfo
    /// builds the objects lists coresponding to a urn
    /// and takes care of object interdependencies
    /// </summary>
    internal class Environment
    {
        XPathExpression m_xpath;
        ArrayList m_listEnumObject;

        public int LastPos
        {
            get { return m_listEnumObject.Count - 1; }
        }

	/// <summary>
        /// build the list of enum objects
        /// </summary>
        /// <param name="urn"></param>
        /// <param name="ci"></param>
        private void GetObjectsFromCache(Urn urn, Object ci)
        {
            m_listEnumObject = ObjectCache.GetAllElements(urn, GetServerVersion(urn, ci), GetDatabaseEngineType(urn, ci), GetDatabaseEngineEdition(urn, ci), ci);

            int i = 0;
            foreach (CacheElement elem in m_listEnumObject)
            {
                elem.EnumObject.Initialize(ci, m_xpath[i++]);
            }
            for (i = m_listEnumObject.Count - 1; i >= 0; i--)
            {
                ((CacheElement)m_listEnumObject[i]).EnumObject.Urn = urn;
                urn = urn.Parent;
            }
        }

        private void PostProcess(EnumResult er)
        {
            for (int i = m_listEnumObject.Count - 1; i >= 0; i--)
            {
                ((CacheElement)m_listEnumObject[i]).EnumObject.PostProcess(er);
            }
        }

        private void PutObjectsInCache()
        {
            ObjectCache.PutAllElements(m_listEnumObject);
        }

        /// <summary>
        /// initialize the objects
        /// take care to call Initialize before asking for parent request
        /// also use revers order as lower objects request form upper objects
        /// </summary>
        /// <param name="req"></param>
        public void InitObjects(Request req)
        {
            Request reqPrevious = req;
            for (int i = m_listEnumObject.Count - 1; i >= 0; i--)
            {
                EnumObject en = ((CacheElement)m_listEnumObject[i]).EnumObject;
                en.Request = reqPrevious;
                reqPrevious = en.RetrieveParentRequest();
            }
        }

        private EnumResult GetData()
        {
            EnumResult er = null;
            foreach (CacheElement ce in m_listEnumObject)
            {
                er = ce.EnumObject.GetData(er);
            }
            return er;
        }

        public EnumResult GetData(Request req, Object ci)
        {
            //compile the m_xpath
            m_xpath = req.Urn.XPathExpression;

            //create and init enumeration objects
            GetObjectsFromCache(req.Urn, ci);
            InitObjects(req);


            EnumResult er = GetData();
            PostProcess(er);

            PutObjectsInCache();

            return er;
        }

	public ObjectInfo GetObjectInfo(Object ci, RequestObjectInfo req)
        {
            //compile the m_xpath
            m_xpath = req.Urn.XPathExpression;

            //load last object and get the results
            CacheElement elem = ObjectCache.GetElement(req.Urn, GetServerVersion(req.Urn, ci), GetDatabaseEngineType(req.Urn, ci), GetDatabaseEngineEdition(req.Urn, ci), ci);
            EnumObject en = elem.EnumObject;

            ObjectInfo oi = new ObjectInfo();
            if (0 != (RequestObjectInfo.Flags.Children & req.InfoType))
            {
                oi.Children = elem.GetChildren();
            }
            if (0 != (RequestObjectInfo.Flags.Properties & req.InfoType))
            {
                oi.Properties = en.GetProperties(ObjectPropertyUsages.All);
            }
            if (0 != (RequestObjectInfo.Flags.UrnProperties & req.InfoType))
            {
                oi.UrnProperties = en.GetUrnProperties();
            }
            if (0 != (RequestObjectInfo.Flags.ResultTypes & req.InfoType))
            {
                oi.ResultTypes = en.ResultTypes;
            }

	    ObjectCache.PutElement(elem);
            return oi;
        }

        internal static ServerVersion GetServerVersion(Urn urn, Object ci)
        {
            if (ci is ServerVersion)
            {
                return (ServerVersion)ci;
            }
            else if (ci is ServerInformation)
            {
                return ((ServerInformation)ci).ServerVersion;
            }
            EnumObject en = ObjectCache.LoadFirstElementVersionless(urn, ci);

            ISupportVersions ic = en as ISupportVersions;
            if (null != ic)
            {
                return ic.GetServerVersion(ci);
            }
            return null;
        }

        public ObjectInfo GetObjectInfo(ServerVersion version, RequestObjectInfo req)
        {
            //compile the m_xpath
            m_xpath = req.Urn.XPathExpression;

            //load last object and get the results
	        CacheElement elem = ObjectCache.GetElement(req.Urn,
                                                        version,

                                                        //as only serverversion has been passed, we can't get engineType or engineEdition
                                                        //hence returning Default.
                                                        DatabaseEngineType.Standalone, 

                                                        DatabaseEngineEdition.Unknown,
                                                        //This method doesn�t have any connection info being passed, and not being used by SFC. 
                                                        //And, there is a corresponding overloaded method that takes the connection info. 
                                                        //The best match for connection in this method is �version�, so passing the same.
                                                        version
                                                        
                                                        ); 
            EnumObject en = elem.EnumObject;


            ObjectInfo oi = new ObjectInfo();
            if (0 != (RequestObjectInfo.Flags.Children & req.InfoType))
            {
                oi.Children = elem.GetChildren();
            }
            if (0 != (RequestObjectInfo.Flags.Properties & req.InfoType))
            {
                oi.Properties = en.GetProperties(ObjectPropertyUsages.All);
            }
            if (0 != (RequestObjectInfo.Flags.UrnProperties & req.InfoType))
            {
                oi.UrnProperties = en.GetUrnProperties();
            }
            if (0 != (RequestObjectInfo.Flags.ResultTypes & req.InfoType))
            {
                oi.ResultTypes = en.ResultTypes;
            }

            ObjectCache.PutElement(elem);
            return oi;
        }

        internal static DatabaseEngineType GetDatabaseEngineType(Urn urn, Object ci)
        {
            if (ci is ServerInformation)
            {
                return ((ServerInformation)ci).DatabaseEngineType;
            }
            EnumObject en = ObjectCache.LoadFirstElementVersionless(urn, ci);

            ISupportDatabaseEngineTypes ic = en as ISupportDatabaseEngineTypes;
            if (null != ic)
            {
                return ic.GetDatabaseEngineType(ci);
            }
            return DatabaseEngineType.Standalone;
        }

        internal static DatabaseEngineEdition GetDatabaseEngineEdition(Urn urn, Object ci)
        {
            if (ci is ServerInformation)
            {
                return ((ServerInformation)ci).DatabaseEngineEdition;
            }
            EnumObject en = ObjectCache.LoadFirstElementVersionless(urn, ci);

            ISupportDatabaseEngineEditions ic = en as ISupportDatabaseEngineEditions;
            if (null != ic)
            {
                return ic.GetDatabaseEngineEdition(ci);
            }
            return DatabaseEngineEdition.Unknown;
        }
    }  
}
    



