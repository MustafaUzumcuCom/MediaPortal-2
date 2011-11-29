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
using System.Runtime.InteropServices;
using DirectShowLib;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Players.Video.Interfaces;
using MediaPortal.UI.Players.Video.Settings;
using MediaPortal.UI.Players.Video.Subtitles;
using MediaPortal.UI.Players.Video.Tools;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.Players.Video
{
  public class TsVideoPlayer : VideoPlayer, ITsReaderCallback, ITsReaderCallbackAudioChange
  {
    #region Imports

    [ComImport, Guid("b9559486-E1BB-45D3-A2A2-9A7AFE49B23F")]
    protected class TsReader { }

    #endregion

    #region Constants and structs

    private const string TSREADER_FILTER_NAME = "TsReader";
    private const int NO_STREAM_INDEX = -1;

    #endregion

    #region Variables

    protected IBaseFilter _fileSource = null;
    protected bool _bMediaTypeChanged = false;
    protected bool _bRequestAudioChange = false;
    SubtitleRenderer _subtitleRenderer;
    IBaseFilter _subtitleFilter;
    protected GraphRebuilder _graphRebuilder;
    protected int _selectedSubtitleIndex = NO_STREAM_INDEX;

    #endregion

    #region Constructor
    /// <summary>
    /// Constructs a TsReader player object.
    /// </summary>
    public TsVideoPlayer()
    {
      PlayerTitle = "TsVideoPlayer"; // for logging
      _requiredCapabilities = CodecHandler.CodecCapabilities.VideoH264 | CodecHandler.CodecCapabilities.VideoMPEG2 | CodecHandler.CodecCapabilities.AudioMPEG;
    }
    #endregion

    #region Graph building
    /// <summary>
    /// Frees the audio/video codecs.
    /// </summary>
    protected override void FreeCodecs()
    {
      // Free subtitle filter
      FilterGraphTools.TryDispose(ref _subtitleRenderer);
      FilterGraphTools.TryRelease(ref _subtitleFilter);
      
      // Free base class
      base.FreeCodecs();

      // Free file source
      FilterGraphTools.TryRelease(ref _fileSource);
    }

    /// <summary>
    /// Adds the file source filter to the graph.
    /// </summary>
    protected override void AddFileSource()
    {
      // Render the file
      _fileSource = (IBaseFilter) new TsReader();

      ITsReader tsReader = (ITsReader) _fileSource;
      tsReader.SetRelaxedMode(1);
      tsReader.SetTsReaderCallback(this);
      tsReader.SetRequestAudioChangeCallback(this);

      _graphBuilder.AddFilter(_fileSource, TSREADER_FILTER_NAME);

      _subtitleRenderer = new SubtitleRenderer(OnTextureInvalidated);
      _subtitleFilter = _subtitleRenderer.AddSubtitleFilter(_graphBuilder);
      if (_subtitleFilter != null)
      {
        _subtitleRenderer.RenderSubtitles = true;
        _subtitleRenderer.SetPlayer(this);
      }

      IFileSourceFilter f = (IFileSourceFilter) _fileSource;
      f.Load(_resourceAccessor.LocalFileSystemPath, null);

      // Init GraphRebuilder
      _graphRebuilder = new GraphRebuilder(_graphBuilder, _fileSource, SyncObj) { PlayerName = PlayerTitle };
    }

    protected override void OnBeforeGraphRunning()
    {
      FilterGraphTools.RenderOutputPins(_graphBuilder, _fileSource);
    }

    /// <summary>
    /// no extension based changes
    /// </summary>
    protected override void SetCapabilitiesByExtension()
    { }


    #endregion

    #region ITSReaderCallback members
    /// <summary>
    /// Callback when MediaType has changed.
    /// </summary>
    /// <param name="mediaType">new MediaType</param>
    /// <returns>0</returns>
    public int OnMediaTypeChanged(int mediaType)
    {
      _graphRebuilder.DoAsynchRebuild();
      return 0;
    }

    /// <summary>
    /// Callback when VideoFormat changed.
    /// </summary>
    /// <param name="streamType"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="aspectRatioX"></param>
    /// <param name="aspectRatioY"></param>
    /// <param name="bitrate"></param>
    /// <param name="isInterlaced"></param>
    /// <returns></returns>
    public int OnVideoFormatChanged(int streamType, int width, int height, int aspectRatioX, int aspectRatioY,
                                    int bitrate, int isInterlaced)
    {
      //_videoFormat.IsValid = true;
      //_videoFormat.streamType = (VideoStreamType)streamType;
      //_videoFormat.width = width;
      //_videoFormat.height = height;
      //_videoFormat.arX = aspectRatioX;
      //_videoFormat.arY = aspectRatioY;
      //_videoFormat.bitrate = bitrate;
      //_videoFormat.isInterlaced = (isInterlaced == 1);
      //Log.Info("TsReaderPlayer: OnVideoFormatChanged - {0}", _videoFormat.ToString());
      return 0;
    }
    #endregion

    #region ITSReaderAudioCallback members

    /// <summary>
    /// Callback when Audio change is requested.
    /// </summary>
    /// <returns></returns>
    public int OnRequestAudioChange()
    {
      _bRequestAudioChange = true;

      EnumerateStreams();
      if (_streamInfoAudio == null || _streamInfoAudio.Count == 0)
        return 0;

      //FIXME: TsReader request an explicit choice for Audio Stream! Otherwise there is a 5 seconds delay, because
      // the demuxer is waiting...
      _streamInfoAudio.EnableStream(_streamInfoAudio[0].Name);
      return 0;
    }

    #endregion

    #region Subtitles

    protected override bool EnumerateStreams()
    {
      //FIXME: TSReader only offers Audio in IAMStreamSelect, it would be cleaner to expose subs as well.
      bool refreshed = base.EnumerateStreams();
      if (refreshed)
      {
        // If base class has refreshed the stream infos, then update the subtitle streams.
        ISubtitleStream subtitleStream = _fileSource as ISubtitleStream;
        int count = 0;
        if (subtitleStream != null)
        {
          _streamInfoSubtitles = new StreamInfoHandler();
          subtitleStream.GetSubtitleStreamCount(ref count);
          if (count > 0)
          {
            StreamInfo subStream = new StreamInfo(null, NO_STREAM_INDEX, NO_SUBTITLES, 0);
            _streamInfoSubtitles.AddUnique(subStream);
          }
          for (int i = 0; i < count; ++i)
          {
            //FIXME: language should be passed back also as LCID
            SubtitleLanguage language = new SubtitleLanguage();
            int type = 0;
            subtitleStream.GetSubtitleStreamLanguage(i, ref language);
            subtitleStream.GetSubtitleStreamType(i, ref type);
            string name = type == 0
                            ? String.Format("{0} (DVB)", language.lang)
                            : String.Format("{0} (Teletext)", language.lang);
            StreamInfo subStream = new StreamInfo(null, i, name, 0);
            _streamInfoSubtitles.AddUnique(subStream);
          }
        }
      }
      return refreshed;
    }

    public override void SetSubtitle(string subtitle)
    {
      EnumerateStreams();
      ISubtitleStream subtitleStream = _fileSource as ISubtitleStream;
      if (_streamInfoSubtitles == null || subtitleStream == null)
        return;

      // First try to find a stream by it's exact LCID.
      StreamInfo streamInfo = _streamInfoSubtitles.FindStream(subtitle);
      if (streamInfo != null)
      {
        // Tell the renderer if it should render subtitles
        _selectedSubtitleIndex = streamInfo.StreamIndex;
        _subtitleRenderer.RenderSubtitles = _selectedSubtitleIndex != NO_STREAM_INDEX;
        if (_selectedSubtitleIndex != NO_STREAM_INDEX)
          subtitleStream.SetSubtitleStream(_selectedSubtitleIndex);
        SaveSubtitlePreference();
      }
    }

    protected override void SaveSubtitlePreference()
    {
      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>() ?? new VideoSettings();
      settings.PreferredSubtitleSteamName = _selectedSubtitleIndex != NO_STREAM_INDEX
        ? Subtitles[_selectedSubtitleIndex] : String.Empty;

      // If selected stream is "No subtitles", we disable the setting
      settings.EnableSubtitles = _selectedSubtitleIndex != NO_STREAM_INDEX;
      ServiceRegistration.Get<ISettingsManager>().Save(settings);
    }

    protected override void SetPreferredSubtitle()
    {
      EnumerateStreams();
      ISubtitleStream subtitleStream = _fileSource as ISubtitleStream;
      if (_streamInfoSubtitles == null || subtitleStream == null)
        return;

      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>() ?? new VideoSettings();

      // first try to find a stream by it's exact LCID.
      StreamInfo streamInfo = _streamInfoSubtitles.FindStream(settings.PreferredSubtitleLanguage) ?? _streamInfoSubtitles.FindSimilarStream(settings.PreferredSubtitleSteamName);
      if (streamInfo == null || !settings.EnableSubtitles)
        // Tell the renderer it should not render subtitles
        _subtitleRenderer.RenderSubtitles = false;
      else
        subtitleStream.SetSubtitleStream(streamInfo.StreamIndex);
    }

    #endregion

    /// <summary>
    /// Render subtitles on video texture if enabled and available.
    /// </summary>
    protected override void PostProcessTexture(Texture targetTexture)
    {
      _subtitleRenderer.DrawOverlay(targetTexture);
    }

    public override TimeSpan CurrentTime
    {
      get { return base.CurrentTime; }
      set
      {
        base.CurrentTime = value;
        if (_subtitleRenderer != null)
          _subtitleRenderer.OnSeek(CurrentTime.TotalSeconds);
      }
    }
  }
}
