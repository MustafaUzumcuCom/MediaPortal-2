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

using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.Common.ResourceAccess
{
  /// <summary>
  /// Public general interface for the media accessor.
  /// </summary>
  public interface IMediaAccessor
  {
    /// <summary>
    /// Collection of all registered local resource providers, organized as a dictionary of
    /// (GUID; provider) mappings.
    /// This resource provider collection is the proposed entry point to get access to physical media
    /// files.
    /// </summary>
    IDictionary<Guid, IResourceProvider> LocalResourceProviders { get; }

    /// <summery>
    /// Returns a collection of all local base resource providers. This is a convenience property for
    /// using <see cref="LocalResourceProviders"/> and filtering and casting them for <see cref="IBaseResourceProvider"/>.
    /// </summery>
    IEnumerable<IBaseResourceProvider> LocalBaseResourceProviders { get; }

    /// <summery>
    /// Returns a collection of all local chained resource providers. This is a convenience property for
    /// using <see cref="LocalResourceProviders"/> and filtering and casting them for <see cref="IChainedResourceProvider"/>.
    /// </summery>
    IEnumerable<IChainedResourceProvider> LocalChainedResourceProviders { get; }

    /// <summary>
    /// Collection of all registered local metadata extractors, organized as a dictionary of
    /// (GUID; metadata extractor) mappings.
    /// </summary>
    IDictionary<Guid, IMetadataExtractor> LocalMetadataExtractors { get; }

    /// <summary>
    /// Initializes resource providers, metadata extractors and internal structures.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Cleans up the runtime data of the media accessor.
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Creates shares for the system's MyMusic, MyVideos and MyPictures directories.
    /// </summary>
    /// <returns>Collection of shares. The shares are not saved to the settings yet.</returns>
    ICollection<Share> CreateDefaultShares();

    /// <summary>
    /// Returns the ids of all local metadata extractors which are classified into the specified
    /// <paramref name="mediaCategory"/>.
    /// </summary>
    /// <param name="mediaCategory">The category to find all local metadata extractors for. If
    /// this parameter is <c>null</c>, the ids of all default metadata extractors are returned,
    /// independent of their category.</param>
    /// <returns>Ids of metadata extractors which can handle the specified <paramref name="mediaCategory"/>.</returns>
    ICollection<Guid> GetMetadataExtractorsForCategory(string mediaCategory);

    /// <summary>
    /// Returns the ids of all local metadata extractors which fill the given media item aspect types.
    /// </summary>
    /// <param name="miaTypeIDs">IDs of media item aspects which should be filled.</param>
    /// <returns>Ids of metadata extractors which fill the specified media item aspects.</returns>
    ICollection<Guid> GetMetadataExtractorsForMIATypes(IEnumerable<Guid> miaTypeIDs);

    /// <summary>
    /// Extracts the specified metadata from the specified local media item.
    /// </summary>
    /// <param name="mediaItemAccessor">Media item file to use as source for this metadata extraction.</param>
    /// <param name="metadataExtractorIds">Enumeration of ids of metadata extractors to apply to the
    /// specified media file.</param>
    /// <param name="forceQuickMode">Specifies if only quick operations for IMetaDataExtractor are allowed.</param>
    /// <returns>Dictionary of (media item aspect id; extracted media item aspect)-mappings or
    /// <c>null</c>, if the specified provider doesn't exist or if no metadata could be extracted.
    /// The result might not contain all media item aspects which can be extracted by the specified resource provider,
    /// if it couldn't extract all of them.</returns>
    IDictionary<Guid, MediaItemAspect> ExtractMetadata(IResourceAccessor mediaItemAccessor,
        IEnumerable<Guid> metadataExtractorIds, bool forceQuickMode);

    /// <summary>
    /// Extracts the specified metadata from the specified local media item.
    /// </summary>
    /// <param name="mediaItemAccessor">Media item file to use as source for this metadata extraction.</param>
    /// <param name="metadataExtractors">Enumeration of metadata extractors to apply to the specified media file.</param>
    /// <param name="forceQuickMode">Specifies if only quick operations for IMetaDataExtractor are allowed.</param>
    /// <returns>Dictionary of (media item aspect id; extracted media item aspect)-mappings or
    /// <c>null</c>, if none of the specified providers could extract any metadata.
    /// The result might not contain all media item aspects which can be extracted by the specified resource provider,
    /// if it couldn't extract all of them.</returns>
    IDictionary<Guid, MediaItemAspect> ExtractMetadata(IResourceAccessor mediaItemAccessor,
        IEnumerable<IMetadataExtractor> metadataExtractors, bool forceQuickMode);

    /// <summary>
    /// Returns a media item for a local resource with metadata extracted by the metadata extractors specified by the
    /// <paramref name="metadataExtractorIds"/> from the specified <paramref name="mediaItemAccessor"/>.
    /// </summary>
    /// <param name="mediaItemAccessor">Accessor describing the media item to extract metadata.</param>
    /// <param name="metadataExtractorIds">Ids of the metadata extractors to employ on the media item.</param>
    /// <returns>Media item with the specified metadata </returns>
    MediaItem CreateLocalMediaItem(IResourceAccessor mediaItemAccessor, IEnumerable<Guid> metadataExtractorIds);
  }
}
