// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Microsoft.SqlServer.Management.Common;


    ///<summary>
    /// The Enumerator class is the front end of this assembly
    ///</summary>
    [ComVisible(false)]
    public class Enumerator
                : MarshalByRefObject

    {
        ///<summary>
        /// enumerator common trace function
        ///</summary>
        [Conditional("DEBUG")]
        static public void TraceInfo(string trace)
        {
            TraceHelper.Trace("w", SQLToolsCommonTraceLvl.L1, "{0}", trace);
        }

        ///<summary>
        /// enumerator common trace function		
        ///</summary>
        [Conditional("DEBUG")]
        static public void TraceInfo(string strFormat, params Object[] arg)
        {
            TraceHelper.Trace("w", SQLToolsCommonTraceLvl.L1, strFormat, arg);
        }

        /// <summary>
        /// retrieve data
        /// </summary>
        /// <param name="connectionInfo">connection to be used</param>
        /// <param name="request">the request that has to be resolved</param>
        /// <returns>results coresponding to the request</returns>
        static public EnumResult GetData(Object connectionInfo, Request request)
        {
            if( null == request )
            {
                throw new ArgumentNullException("request");
            }
            if( null == request.Urn )
            {
                throw new ArgumentNullException("request.Urn");
            }

            ConnectionHelpers.UpdateConnectionInfoIfContainedAuthentication(ref connectionInfo, request.Urn);

#if DEBUGTRACE
            string s = request.Urn;
            if( null != request.Fields )
            {
                s += '\t';
                foreach(string f in request.Fields)
                {
                    s += f + ' ';
                }
            }

            Enumerator.TraceInfo("Received request:\n{0}\n", s);
#endif
            Request req = request.ShallowClone();
            int propertiesRequestedCount = 0;

            req.Fields = FixPropertyList(connectionInfo, req.Urn, req.Fields, request.RequestFieldsTypes);
            req.RequestFieldsTypes = RequestFieldsTypes.Request;
            propertiesRequestedCount += req.Fields.Length;

            if( null != req.ParentPropertiesRequests )
            {
                Urn urn = req.Urn;
                foreach(PropertiesRequest pr in req.ParentPropertiesRequests)
                {
                    if( null == urn )
                    {
                        break;
                    }
                    urn = urn.Parent;
                    if( null != pr )
                    {
                        pr.Fields = FixPropertyList(connectionInfo, urn, pr.Fields, pr.RequestFieldsTypes);
                        pr.RequestFieldsTypes = RequestFieldsTypes.Request;
                        propertiesRequestedCount += pr.Fields.Length;
                    }
                }
            }

            if (0 == propertiesRequestedCount)
            {
                throw new QueryNotSupportedEnumeratorException(SfcStrings.NoPropertiesRequested);
            }

            EnumResult result = new Environment().GetData(req, connectionInfo);

            Enumerator.TraceInfo("Serving response for request:\n{0}\n", request.Urn);

            return result;
        }

        /// <summary>
        /// Registers an enumerator extension.
        /// </summary>
        /// <param name="urn">Parent urn or null if root.</param>
        /// <param name="name">Name of type.</param>
        /// <param name="assembly">Assembly reference containing implementsType.</param>
        /// <param name="implementsType">Type that implements the specified enumerator level.</param>
        public static void RegisterExtension(Urn urn, string name, Assembly assembly, string implementsType)
        {
            ObjectLoadInfoManager.AddExtension(urn, name, assembly, implementsType);
        }

        /// <summary>
        /// retrieve data
        /// </summary>
        /// <param name="connectionInfo">connection</param>
        /// <param name="urn">the xpath expresion</param>
        /// <returns>result - all fields except expensive no order</returns>
        static public EnumResult GetData(Object connectionInfo, Urn urn)
        {
            return new Enumerator().Process(connectionInfo, new Request(urn));
        }

        /// <summary>
        /// retrieve data
        /// </summary>
        /// <param name="connectionInfo">connection</param>
        /// <param name="urn">the xpath expresion</param>
        /// <param name="requestedFields">list of requested fields</param>
        /// <returns>result - requested fields no order</returns>
        static public EnumResult GetData(Object connectionInfo, Urn urn, String[] requestedFields)
        {
            return new Enumerator().Process(connectionInfo, new Request(urn, requestedFields));
        }

        /// <summary>
        /// retrieve data
        /// </summary>
        /// <param name="connectionInfo">connection</param>
        /// <param name="urn">the xpath expresion</param>
        /// <param name="requestedFields">list of requested fields</param>
        /// <param name="orderBy">order by the listed fields in the specified order</param>
        /// <returns>result - requested fields no order</returns>
        static public EnumResult GetData(Object connectionInfo, Urn urn, String[] requestedFields, OrderBy[] orderBy)
        {
            return new Enumerator().Process(connectionInfo, new Request(urn, requestedFields, orderBy));
        }

        /// <summary>
        /// retrieve data
        /// </summary>
        /// <param name="connectionInfo">connection</param>
        /// <param name="urn">the xpath expresion</param>
        /// <param name="requestedFields">list of requested fields</param>
        /// <param name="orderBy">order by a particular field in the specified order</param>
        /// <returns>result - requested fields no order</returns>
        static public EnumResult GetData(Object connectionInfo, Urn urn, String[] requestedFields, OrderBy orderBy)
        {
            return new Enumerator().Process(connectionInfo, new Request(urn, requestedFields, new OrderBy [] { orderBy }));
        }


        /// <summary>
        /// back comp function <see>GetData</see>
        /// this function also takes care updating the connectionInfo based on the request
        /// to handle requests over a Cloud DB connection
        /// </summary>
        /// <param name="connectionInfo"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public EnumResult Process(Object connectionInfo, Request request)
        {
            bool isUpdated = false;
            Object inputConnectionInfo = connectionInfo;
            try
            {
                isUpdated = ConnectionHelpers.UpdateConnectionInfoIfCloud(ref connectionInfo, request.Urn);
                return Enumerator.GetData(connectionInfo, request);
            }
            catch (Exception e)
            {
                EnumeratorException.FilterException(e);
                if (isUpdated)
                {
                    //The exception might have occured due to some LPU scenario
                    // like if the user doesn't have the master database access
                    // if so process without updating the connection info
                    return this.VanillaProcess(inputConnectionInfo, request);
                }
                throw new EnumeratorException(SfcStrings.FailedRequest, e);
            }
        }


        /// <summary>
        ///    process without updating the connection info
        /// </summary>
        /// <param name="connectionInfo"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private EnumResult VanillaProcess(Object connectionInfo, Request request)
        {
            try
            {               
                return Enumerator.GetData(connectionInfo, request);
            }
            catch (Exception e)
            {
                EnumeratorException.FilterException(e);
                throw new EnumeratorException(SfcStrings.FailedRequest, e);
            }
        }

        /// <summary>
        /// Get Object info for the level
        /// </summary>
        /// <param name="connectionInfo">connectin info - for the server version</param>
        /// <param name="urn">level specifier</param>
        /// <returns>level information - all information is returned</returns>
        static internal ObjectInfo GetObjectInfo(Object connectionInfo, Urn urn)
        {
            return Enumerator.GetObjectInfo(connectionInfo, new RequestObjectInfo(urn, RequestObjectInfo.Flags.All));
        }

        /// <summary>
        /// Get Object info for the level
        /// </summary>
        /// <param name="connectionInfo">connectin info - for the server version</param>
        /// <param name="urn">level specifier</param>
        /// <param name="flags">restrit amount the information returned</param>
        /// <returns>level information</returns>
        static internal ObjectInfo GetObjectInfo(Object connectionInfo, Urn urn, RequestObjectInfo.Flags flags)
        {
            return Enumerator.GetObjectInfo(connectionInfo, new RequestObjectInfo(urn, flags));
        }

        /// <summary>
        /// get object info for the level
        /// </summary>
        /// <param name="connectionInfo">connectin info - for the server version</param>
        /// <param name="requestObjectInfo">specify the request</param>
        /// <returns>level information</returns>
        static internal ObjectInfo GetObjectInfo(Object connectionInfo, RequestObjectInfo requestObjectInfo)
        {
            if( null == requestObjectInfo )
            {
                throw new ArgumentNullException("requestObjectInfo");
            }
            if( null == requestObjectInfo.Urn )
            {
                throw new ArgumentNullException("requestObjectInfo.Urn");
            }


            Enumerator.TraceInfo("Received object info request:\n{0}\n", requestObjectInfo.Urn);

            ObjectInfo oi = new Environment().GetObjectInfo(connectionInfo, requestObjectInfo);

            Enumerator.TraceInfo( "Serving response for object info request:\n{0}\n", requestObjectInfo.Urn);

            return oi;
        }

        /// <summary>
        /// back comp function <see>GetObjectInfo</see>
        /// </summary>
        /// <param name="connectionInfo"></param>
        /// <param name="requestObjectInfo"></param>
        /// <returns></returns>
        public ObjectInfo Process(Object connectionInfo, RequestObjectInfo requestObjectInfo)
        {
            // This is required after SQL DW integration as previously the object related information was collected based on version and serverType 
            // but now we also need databaseEngineEdition for that.
            ConnectionHelpers.UpdateConnectionInfoIfCloud(ref connectionInfo, requestObjectInfo.Urn);
            return Enumerator.GetObjectInfo(connectionInfo, requestObjectInfo);
        }

        public ObjectInfo Process (ServerVersion version, RequestObjectInfo requestObjectInfo)
        {
            return new Environment ().GetObjectInfo (version, requestObjectInfo);
        }

        /// <summary>
        /// enumerates dependencies for the specified objects as a generalize tree
        /// </summary>
        /// <param name="connectionInfo">server connection info</param>
        /// <param name="dependencyRequest">list of XPATHS for which the dependency must be discovered + aditional flags
        /// note each xpath can have multiple results</param>
        /// <returns>list of dependencies for each input record</returns>
        public DependencyChainCollection EnumDependencies(Object connectionInfo, DependencyRequest dependencyRequest)
        {
            if (null == connectionInfo)
            {
                throw new ArgumentNullException("connectionInfo");
            }
            if (null == dependencyRequest)
            {
                throw new ArgumentNullException("dependencyRequest");
            }

            if (null == dependencyRequest.Urns || 0 == dependencyRequest.Urns.Length)
            {
                return new DependencyChainCollection();
            }

#if SSMS_EXPRESS			
            IEnumDependencies ied = ObjectCache.CreateObjectInstance("Microsoft.SqlServer.Express.SqlEnum", 
                            "Microsoft.SqlServer.Management.Smo.SqlEnumDependencies") as IEnumDependencies;
#else
            IEnumDependencies ied = ObjectCache.CreateObjectInstance("Microsoft.SqlServer.SqlEnum",
                            "Microsoft.SqlServer.Management.Smo.SqlEnumDependencies") as IEnumDependencies;
#endif

            return ied.EnumDependencies(connectionInfo, dependencyRequest);
        }


        /// <summary>
        /// compute the list of properties that we are going to request 
        /// </summary>
        /// <param name="connectionInfo"></param>
        /// <param name="urn"></param>
        /// <param name="fields"></param>
        /// <param name="requestFieldsType"></param>
        /// <returns>the list of properties that will be returned to the user</returns>
        static private String[] FixPropertyList(Object connectionInfo, Urn urn, String[] fields, RequestFieldsTypes requestFieldsType)
        {
            // for RequestFieldsTypes.Request with fields specified we're done
            if( null != fields && 0 != (RequestFieldsTypes.Request & requestFieldsType) )
            {
                return fields;
            }

            // go get all the properties that this object has
            RequestObjectInfo roi = new RequestObjectInfo();
            roi.Urn = urn;
            roi.InfoType = RequestObjectInfo.Flags.Properties;
            ObjectInfo oi = Enumerator.GetObjectInfo(connectionInfo, roi);

            //place holder for building properties list
            ArrayList props = new ArrayList();
            bool addExpensive = (0 != (RequestFieldsTypes.IncludeExpensiveInResult & requestFieldsType));
 
            //reject with no properties specified means we request all properties
            //request with no properties specified would mean we request nothing, reverse it to mean we request everything
            if( null == fields )
            {
                foreach(ObjectProperty op in oi.Properties)
                {
                    //return the expensive ones only if requested
                    if( (!op.Expensive || addExpensive) && 
                        ObjectPropertyUsages.Request == (op.Usage & ObjectPropertyUsages.Request) )
                    {
                        props.Add(op.Name);
                    }
                }
            }
            else //reject with null specified
            {
                //remember rejected properties in a hastable
                Hashtable rejectList = new Hashtable();
                foreach (string propName in fields)
                {
                    rejectList[propName] = null;
                }
                foreach(ObjectProperty op in oi.Properties)
                {
                    if( !rejectList.ContainsKey(op.Name) )
                    {
                        //return the expensive ones only if requested
                        if( !op.Expensive || addExpensive )
                        {
                            props.Add(op.Name);
                        }
                    }
                }
            }

            string[] retArr = new string[props.Count];
            props.CopyTo(retArr, 0);
            return retArr;
        }
    }
}
