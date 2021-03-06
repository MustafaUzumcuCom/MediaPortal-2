﻿#region Copyright (C) 2007-2011 Team MediaPortal

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

using System;
using System.Drawing;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Rendering;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.Controls.ImageSources
{
  /// <summary>
  /// <see cref="MediaItemSource"/> acts as a source provider / renderer for the <see cref="Visuals.Image"/> control.
  /// It's extends the <see cref="BitmapImageSource"/> to support thumbnail building for MediaItems.
  /// </summary>
  public class MediaItemSource : TextureImageSource
  {
    #region Protected fields

    protected byte[] _thumbBinary = null;
    protected string _key;
    protected int _thumbnailSize;
    protected TextureAsset _texture = null;
    protected RightAngledRotation _rotation = RightAngledRotation.Zero;
    protected bool _flipX = false;
    protected bool _flipY = false;

    #endregion

    #region Constructor

    /// <summary>
    /// Constructs a <see cref="MediaItemSource"/> for building thumbnails for MediaItems.
    /// </summary>
    /// <param name="mediaItem">MediaItem to create thumbnail.</param>
    /// <param name="thumbnailSize">Requested thumbnail size.</param>
    public MediaItemSource(MediaItem mediaItem, int thumbnailSize)
    {
      _thumbnailSize = thumbnailSize;
      Guid id = mediaItem.MediaItemId;
      // Local media items don't have an item id
      _key = (id == Guid.Empty ? Guid.NewGuid() : id).ToString();

      // Each resolution is cached separately. If we read cache only and our favourite resolution is not yet in cache,
      // we try to find any other existing.
      if (thumbnailSize <= 96)
      {
        if (mediaItem.Aspects.ContainsKey(ThumbnailSmallAspect.ASPECT_ID))
          _thumbBinary = (byte[]) mediaItem.Aspects[ThumbnailSmallAspect.ASPECT_ID].GetAttributeValue(ThumbnailSmallAspect.ATTR_THUMBNAIL);

        if (_thumbBinary == null && mediaItem.Aspects.ContainsKey(ThumbnailLargeAspect.ASPECT_ID))
          _thumbBinary = (byte[]) mediaItem.Aspects[ThumbnailLargeAspect.ASPECT_ID].GetAttributeValue(ThumbnailLargeAspect.ATTR_THUMBNAIL);
      }
      else
      {
        if (mediaItem.Aspects.ContainsKey(ThumbnailLargeAspect.ASPECT_ID))
          _thumbBinary = (byte[]) mediaItem.Aspects[ThumbnailLargeAspect.ASPECT_ID].GetAttributeValue(ThumbnailLargeAspect.ATTR_THUMBNAIL);
        
        if (_thumbBinary == null && mediaItem.Aspects.ContainsKey(ThumbnailSmallAspect.ASPECT_ID))
          _thumbBinary = (byte[]) mediaItem.Aspects[ThumbnailSmallAspect.ASPECT_ID].GetAttributeValue(ThumbnailSmallAspect.ATTR_THUMBNAIL);
      }
      PictureRotation rotation;
      PictureAspect.GetOrientationMetadata(mediaItem, out rotation, out _flipX, out _flipY);
      _rotation = RotationTranslator.TranslateToRightAngledRotation(rotation);
    }

    #endregion

    #region ImageSource implementation

    public override bool IsAllocated
    {
      get { return _texture != null && _texture.IsAllocated; }
    }

    protected override Texture Texture
    {
      get { return _texture == null ? null : _texture.Texture; }
    }

    protected override SizeF RawSourceSize
    {
      get { return (_texture != null && _texture.IsAllocated) ? new SizeF(_texture.Width, _texture.Height) : SizeF.Empty; }
    }

    protected override RectangleF TextureClip
    {
      get { return _texture == null ? RectangleF.Empty : new RectangleF(0, 0, _texture.MaxU, _texture.MaxV); }
    }

    public override void Allocate()
    {
      if (_texture == null && _thumbBinary != null)
        _texture = ContentManager.Instance.GetTexture(_thumbBinary, _key);
      if (_texture != null && !_texture.IsAllocated)
      {
        _texture.Allocate();
        if (_texture.IsAllocated)
        {
          _imageContext.Refresh();
          _imageContext.Rotation = _rotation;
          FireChanged();
        }
      }
    }

    #endregion

    #region Protected methods

    protected override void FreeData()
    {
      _texture = null;
      base.FreeData();
    }

    #endregion
  }
}
