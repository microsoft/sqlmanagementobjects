// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Common
{
    public interface ICreatable
    {
        void Create();
    }

    /// <summary>
    /// Interface for CreateOrAlter method. All classes that support create or alter
    /// implement this interface.
    /// </summary>
    public interface ICreateOrAlterable
    {
        /// <summary>
        /// Create OR ALTER the object.
        /// First perform setup for execution
        /// Then validate "CREATE OR ALTER" syntax through ScriptCreateOrAlter(),
        /// Finally execute the query and do cleanup.
        /// return without exception.
        /// </summary>
        void CreateOrAlter();
    }

    public interface IDroppable
    {
        void Drop();
    }

    /// <summary>
    /// Interface for DropIfExists method. All classes that support drop with existence check
    /// implement this interface.
    /// </summary>
    public interface IDropIfExists
    {
        /// <summary>
        /// Drops the object with IF EXISTS option. If object is invalid for drop function will
        /// return without exception.
        /// </summary>
        void DropIfExists();
    }

    public interface IAlterable
    {
        void Alter();
    }

    /// <summary>
    /// Implemented by objects that can be dropped as part of the Alter command on their parent.
    /// </summary>
    public interface IMarkForDrop
    {
        /// <summary>
        /// Sets the object in either ToBeDropped or Existing state
        /// </summary>
        /// <param name="dropOnAlter">When true and the current object state is Existing, sets object state to ToBeDropped when its parent's Alter method is called.
        /// When false and the current object state is ToBeDropped, sets the object state to Existing</param>
        void MarkForDrop(bool dropOnAlter);
    }

    public interface IRenamable
    {
        void Rename(string newname);
    }
    
    public interface IRestrictedAccess
    {
        bool SingleConnection { get; set; }
    }

    public interface IRefreshable
    {
        void Refresh ();
    }

    /// <summary>
    /// Implemented by objects that may prefer to have the UI confirm the rename before proceeding
    /// </summary>
    public interface ISafeRenamable : IRenamable
    {
        bool WarnOnRename { get; }
    }
}

