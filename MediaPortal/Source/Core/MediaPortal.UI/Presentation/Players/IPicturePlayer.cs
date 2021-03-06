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

using System.Drawing;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.UI.Presentation.Players
{
  public enum RightAngledRotation
  {
    Zero,
    HalfPi,
    Pi,
    ThreeHalfPi
  }

  /// <summary>
  /// Interface for a picture player. Holds all methods which are common to all picture players.
  /// </summary>
  public interface IPicturePlayer : IPlayer
  {
    IResourceLocator CurrentPictureResourceLocator { get; }
    
    /// <summary>
    /// Returns the size of the whole picture.
    /// </summary>
    Size PictureSize { get; }

    /// <summary>
    /// Returns the rotation which must be applied to the picture.
    /// </summary>
    RightAngledRotation Rotation { get; }

    /// <summary>
    /// Returns if the picture must be flipped in horiziontal direction after the <see cref="Rotation"/>.
    /// </summary>
    bool FlipX { get; }

    /// <summary>
    /// Returns if the picture must be flipped in vertical direction after the <see cref="Rotation"/>.
    /// </summary>
    bool FlipY { get; }

    bool SlideShowEnabled { get; set; }
  }
}