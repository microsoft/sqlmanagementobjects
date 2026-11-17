// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    partial class SmoMetadataProvider
    {
        private sealed class DisconnectedSmoMetadataProvider : SmoMetadataProvider
        {
            public static DisconnectedSmoMetadataProvider Create(Smo.Server server)
            {
                if (server == null) throw new ArgumentNullException("server");

                return new DisconnectedSmoMetadataProvider(server);
            }

            private DisconnectedSmoMetadataProvider(Smo.Server server)
                : base(server, false)
            {
            }

            /// <summary>
            /// Gets the method that will handle BeforeBind event.
            /// </summary>
            public override MetadataProviderEventHandler BeforeBindHandler
            {
                get { return null; }
            }

            /// <summary>
            /// Gets the method that will handle AfterBind event.
            /// </summary>
            public override MetadataProviderEventHandler AfterBindHandler
            {
                get { return null; }
            }
        }
    }
}
