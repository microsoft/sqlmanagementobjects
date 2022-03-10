// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// This class holds global Sfc events
    /// </summary>
    public class SfcApplicationEvents
    {
        public event SfcApplication.SfcObjectCreatedEventHandler ObjectCreated;
        public event SfcApplication.SfcObjectAlteredEventHandler ObjectAltered;
        public event SfcApplication.SfcObjectDroppedEventHandler ObjectDropped;
        public event SfcApplication.SfcBeforeObjectRenamedEventHandler BeforeObjectRenamed;
        public event SfcApplication.SfcAfterObjectRenamedEventHandler AfterObjectRenamed;
        public event SfcApplication.SfcBeforeObjectMovedEventHandler BeforeObjectMoved;
        public event SfcApplication.SfcAfterObjectMovedEventHandler AfterObjectMoved;

        public void OnObjectCreated(SfcInstance obj, SfcObjectCreatedEventArgs e)
        {
            if (ObjectCreated != null)
            {
                ObjectCreated((object)obj, e);
            }
        }

        public void OnObjectAltered(SfcInstance obj, SfcObjectAlteredEventArgs e)
        {
            if (ObjectAltered != null)
            {
                ObjectAltered((object)obj, e);
            }
        }

        public void OnObjectDropped(SfcInstance obj, SfcObjectDroppedEventArgs e)
        {
            if (ObjectDropped != null)
            {
                ObjectDropped((object)obj, e);
            }
        }

        public void OnBeforeObjectRenamed(SfcInstance obj, SfcBeforeObjectRenamedEventArgs e)
        {
            if (BeforeObjectRenamed != null)
            {
                BeforeObjectRenamed((object)obj, e);
            }
        }

        public void OnAfterObjectRenamed(SfcInstance obj, SfcAfterObjectRenamedEventArgs e)
        {
            if (AfterObjectRenamed != null)
            {
                AfterObjectRenamed((object)obj, e);
            }
        }

        public void OnBeforeObjectMoved(SfcInstance obj, SfcBeforeObjectMovedEventArgs e)
        {
            if (BeforeObjectMoved != null)
            {
                BeforeObjectMoved((object)obj, e);
            }
        }

        public void OnAfterObjectMoved(SfcInstance obj, SfcAfterObjectMovedEventArgs e)
        {
            if (AfterObjectMoved != null)
            {
                AfterObjectMoved((object)obj, e);
            }
        }

    }

    /// <summary>
    /// The Sfc system and its global events and data.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1053:StaticHolderTypesShouldNotHaveConstructors")]
    public class SfcApplication
    {
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly SfcApplicationEvents Events = new SfcApplicationEvents();

        internal static readonly string ModuleName = "Sfc";

        #region Internal events
        /// <summary>
        /// Called when an object is successfully created
        /// </summary>
        public delegate void SfcObjectCreatedEventHandler(object sender, SfcObjectCreatedEventArgs e);

        /// <summary>
        /// called when an object is successfully dropped
        /// </summary>
        public delegate void SfcObjectDroppedEventHandler(object sender, SfcObjectDroppedEventArgs e);

        /// <summary>
        /// called when an object is successfully renamed, before the client-side updating
        /// </summary>
        public delegate void SfcBeforeObjectRenamedEventHandler(object sender, SfcBeforeObjectRenamedEventArgs e);

        /// <summary>
        /// called when an object is successfully renamed, after the client-side updating
        /// </summary>
        public delegate void SfcAfterObjectRenamedEventHandler(object sender, SfcAfterObjectRenamedEventArgs e);

        /// <summary>
        /// called when an object is successfully moved, before the client-side updating
        /// </summary>
        public delegate void SfcBeforeObjectMovedEventHandler(object sender, SfcBeforeObjectMovedEventArgs e);

        /// <summary>
        /// called when an object is successfully moved, after the client-side updating
        /// </summary>
        public delegate void SfcAfterObjectMovedEventHandler(object sender, SfcAfterObjectMovedEventArgs e);

        /// <summary>
        /// called when an object is successfully altered
        /// </summary>
        public delegate void SfcObjectAlteredEventHandler(object sender, SfcObjectAlteredEventArgs e);
        #endregion
    }

    #region Event arguments
    /// <summary>
    /// Base argument class for Sfc events
    /// </summary>
    public class SfcEventArgs : System.EventArgs
    {
        private Urn urn;
        /// <summary>
        /// The current urn of the current object.
        /// </summary>
        public Urn Urn
        {
            get { return urn; }
        }

        private SfcInstance instance;
        /// <summary>
        /// The current object.
        /// </summary>
        public SfcInstance Instance
        {
            get { return instance; }
        }
        public SfcEventArgs(Urn urn, SfcInstance instance)
        {
            this.urn = urn;
            this.instance = instance;
        }
    }

    /// <summary>
    /// Event arguments passed when an object is created.
    /// </summary>
    public class SfcObjectCreatedEventArgs : SfcEventArgs
    {
        public SfcObjectCreatedEventArgs(Urn urn, SfcInstance instance)
            : base(urn, instance)
        {
        }
    }

    /// <summary>
    /// Event arguments passed when an object is altered.
    /// </summary>
    public class SfcObjectAlteredEventArgs : SfcEventArgs
    {
        public SfcObjectAlteredEventArgs(Urn urn, SfcInstance instance)
            : base(urn, instance)
        {
        }
    }

    /// <summary>
    /// Event arguments passed when an object is dropped.
    /// </summary>
    public class SfcObjectDroppedEventArgs : SfcEventArgs
    {
        public SfcObjectDroppedEventArgs(Urn urn, SfcInstance instance)
            : base(urn, instance)
        {
        }
    }

    /// <summary>
    /// Event arguments passed when an object is successfully renamed, before the client-side updating.
    /// </summary>
    public class SfcBeforeObjectRenamedEventArgs : SfcEventArgs
    {
        private Urn newUrn;
        private SfcKey newKey;

        /// <summary>
        /// The new Urn of the object.
        /// </summary>
        public Urn NewUrn
        {
            get { return newUrn; }
        }

        /// <summary>
        /// The new Key of the object.
        /// </summary>
        public SfcKey NewKey
        {
            get { return newKey; }
        }
        public SfcBeforeObjectRenamedEventArgs(Urn urn, SfcInstance instance, Urn newUrn, SfcKey newKey)
            : base(urn, instance)
        {
            this.newUrn= newUrn;
            this.newKey = newKey;
        }
    }

    /// <summary>
    /// Event arguments passed when an object is successfully renamed, after the client-side updating.
    /// </summary>
    public class SfcAfterObjectRenamedEventArgs : SfcEventArgs
    {
        private Urn oldUrn;
        private SfcKey oldKey;

        /// <summary>
        /// The old urn of the object.
        /// </summary>
        public Urn OldUrn
        {
            get { return oldUrn; }
        }

        /// <summary>
        /// The old Key of the object.
        /// </summary>
        public SfcKey OldKey
        {
            get { return oldKey; }
        }
        public SfcAfterObjectRenamedEventArgs(Urn urn, SfcInstance instance, Urn oldUrn, SfcKey oldKey)
            : base(urn, instance)
        {
            this.oldUrn = oldUrn;
            this.oldKey = oldKey;
        }
    }

    /// <summary>
    /// Event arguments passed when an object is successfully moved, before the client-side updating.
    /// </summary>
    public class SfcBeforeObjectMovedEventArgs : SfcEventArgs
    {
        private Urn newUrn;
        private SfcInstance newParent;

        /// <summary>
        /// The new urn of the object.
        /// </summary>
        public Urn NewUrn
        {
            get { return newUrn; }
        }

        /// <summary>
        /// The new parent object of the object.
        /// </summary>
        public SfcInstance NewParent
        {
            get { return newParent; }
        }
        public SfcBeforeObjectMovedEventArgs(Urn urn, SfcInstance instance, Urn newUrn, SfcInstance newParent)
            : base(urn, instance)
        {
            this.newUrn = newUrn;
            this.newParent = newParent;
        }
    }

/// <summary>
    /// Event arguments passed when an object is successfully moved, after the client-side updating.
    /// </summary>
    public class SfcAfterObjectMovedEventArgs : SfcEventArgs
    {
        private Urn oldUrn;
        private SfcInstance oldParent;

        /// <summary>
        /// The old urn of the object.
        /// </summary>
        public Urn OldUrn
        {
            get { return oldUrn; }
        }

        /// <summary>
        /// The old parent object of the object.
        /// </summary>
        public SfcInstance OldParent
        {
            get { return oldParent; }
        }
        public SfcAfterObjectMovedEventArgs(Urn urn, SfcInstance instance, Urn oldUrn, SfcInstance oldParent)
            : base(urn, instance)
        {
            this.oldUrn = oldUrn;
            this.oldParent = oldParent;
        }
    }

#endregion

}
