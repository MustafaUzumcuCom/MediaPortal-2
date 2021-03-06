#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using MediaPortal.UI.Services.UserManagement;

namespace MediaPortal.UI.UserManagement
{
  /// <summary>
  /// Interface for a permission. A permission may be an atomic right which can be assigned or revoked to a user/role,
  /// or may be fleshed out by adding concrete permission objects.
  /// </summary>
  public interface IPermission
  {
    /// <summary>
    /// Gets or sets the name of this permission.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Adds a permisson object to this permission (e.g. a MediaItem).
    /// </summary>
    /// <param name="item">Ihe item to be added.</param>
    /// <returns><c>true</c>, if the permission object could successfully be added, else<c>false</c>.</returns>
    bool AddPermissionObject(IPermissionObject item);

    /// <summary>
    /// Removes a permisson object from this permission.
    /// </summary>
    /// <param name="item">The item to be removed.</param>
    /// <returns><c>true</c>, if the permission object could successfully be removed, else<c>false</c>.</returns>
    bool RemovePermissionObject(IPermissionObject item);

    /// <summary>
    /// Gets all permission objects that are assigned to this permission.
    /// </summary>
    /// <returns>List of permission objects.</returns>
    IList<IPermissionObject> GetPermissionObjects();

    /// <summary>
    /// Checks if this permission is applicable of the specified permission object.
    /// </summary>
    /// <param name="item">The item to check permission for.</param>
    /// <returns><c>true</c>, if this permission includes the permission on the specified item, else <c>false</c>.</returns>
    bool IncludesPermissionOn(IPermissionObject item);
  }
}
