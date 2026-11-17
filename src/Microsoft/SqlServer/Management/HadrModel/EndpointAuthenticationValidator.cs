// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.HadrData;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Validate whether the on-premise endpoints use Certificate authentication.
    /// </summary>
    public class EndpointAuthenticationValidator : Validator
    {
        /// <summary>
        /// availabilityGroupData object contains information for the Availability Group
        /// </summary>
        private AvailabilityGroupData availabilityGroupData;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="agData">Availability Group Data</param>
        public EndpointAuthenticationValidator(AvailabilityGroupData agData)
            : base("Checking if existing endpoints support Windows authentication")
        {
            if (agData == null)
            {
                throw new ArgumentNullException("AvailabilityGroupData");
            }

            this.availabilityGroupData = agData;
        }

        /// <summary>
        /// The validate method
        /// </summary>
        protected override void Validate(IExecutionPolicy policy)
        {
            // if the number of replicas in the AG is less than 1, there is no need for EndipointAuthentication Validation
            if (this.availabilityGroupData.AvailabilityGroupReplicas.Count < 1)
            {
                policy.Expired = true;
            }

            foreach (AvailabilityGroupReplica replica in this.availabilityGroupData.AvailabilityGroupReplicas)
            {
                Smo.Server server = replica.GetServer();

                if (server == null || server.ConnectionContext == null)
                {
                    return;
                }

                foreach (Smo.Endpoint e in server.Endpoints)
                {
                    if (e.EndpointType == Microsoft.SqlServer.Management.Smo.EndpointType.DatabaseMirroring)
                    {
                        Smo.EndpointAuthenticationOrder endpointAuthenticationOrder = e.Payload.DatabaseMirroring.EndpointAuthenticationOrder;

                        // we only use Negotiate authentication for WA replica, so validation fails here
                        if (endpointAuthenticationOrder == Smo.EndpointAuthenticationOrder.Certificate)
                        {
                            //validation fail
                            policy.Expired = true;

                            throw new EndpointAuthenticationValidationException(replica.EndpointName, endpointAuthenticationOrder.ToString());
                        }

                        break;
                    }
                }

                // no exception, validation success
                policy.Expired = true;
            }
        }

    }
}
