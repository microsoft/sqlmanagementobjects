// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// Inform Sfc that this object is scripted by its parent and does not generate its own CRUD scripting.
    /// There are no methods to implement, this is a sentinel interface.
    /// </summary>
    public interface IScriptedByParent
    {
    }

    public interface ISfcCreatable : Common.ICreatable
    {
        /// <summary>
        /// Produce the script to create this object to storage in its current state.
        /// </summary>
        /// <returns></returns>
        ISfcScript ScriptCreate();
    }

    public interface ISfcDroppable : Common.IDroppable
    {
        /// <summary>
        /// Produce the script to drop this object from storage.
        /// </summary>
        /// <returns></returns>
        ISfcScript ScriptDrop();
    }

    public interface ISfcMarkForDrop : Common.IMarkForDrop
    {
    }

    public interface ISfcAlterable : Common.IAlterable
    {
        /// <summary>
        /// Produce the script to alter the storage to match the current object state.
        /// </summary>
        /// <returns></returns>
        ISfcScript ScriptAlter();
    }

    /// For single string-oriented renaming use Common.IRenamable
    public interface ISfcRenamable
    {
        /// <summary>
        /// Rename this object to the given key and update storage to reflect it.
        /// This assumes the object can logically rename itself based on all or part of the key data,
        /// and Sfc internally will adjust the object's Key and collection membership.
        /// A rename event will fire after internal updates succeed to allow listeners to adjust
        /// similar external collections and such.
        /// For single string-oriented renaming see ISfcRenamable.
        /// For KeyChain-oriented moving see ISfcMovable.
        /// </summary>
        /// <returns></returns>
        void Rename(SfcKey newKey);

        /// <summary>
        /// Produce the script to rename this object to the given key.
        /// This assumes the object can logically rename itself based on all or part of the key data.
        /// For single string-oriented renaming see ISfcRenamable.
        /// For KeyChain-oriented moving see ISfcMovable.
        /// </summary>
        /// <returns>The script to rename this object</returns>
        ISfcScript ScriptRename(SfcKey newKey);
    }

    public interface ISfcMovable
    {
        /// <summary>
        /// Move this object under the given parent object and update storage to reflect it.
        /// </summary>
        /// <returns></returns>
        void Move(SfcInstance newParent);

        /// <summary>
        /// Produce the script to move this object under the given parent object.
        /// </summary>
        /// <returns>The script to move this object</returns>
        ISfcScript ScriptMove(SfcInstance newParent);
    }
}
