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
using MediaPortal.Common.General;
using MediaPortal.Plugins.SlimTvClient.Interfaces.Items;

namespace MediaPortal.Plugins.SlimTvClient.Helpers
{
  /// <summary>
  /// ProgramProperties acts as GUI wrapper for an IProgram instance to allow binding of Properties.
  /// </summary>
  public class ProgramProperties
  {
    private DateTime _viewPortMinTime;
    private DateTime _viewPortMaxTime;

    public AbstractProperty TitleProperty { get; set; }
    public AbstractProperty DescriptionProperty { get; set; }
    public AbstractProperty StartTimeProperty { get; set; }
    public AbstractProperty EndTimeProperty { get; set; }
    public AbstractProperty RemainingDurationProperty { get; set; }
    public AbstractProperty GenreProperty { get; set; }

    /// <summary>
    /// Gets or Sets the Title.
    /// </summary>
    public String Title
    {
      get { return (String)TitleProperty.GetValue(); }
      set { TitleProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or Sets the Long Description.
    /// </summary>
    public String Description
    {
      get { return (String)DescriptionProperty.GetValue(); }
      set { DescriptionProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or Sets the Genre.
    /// </summary>
    public String Genre
    {
      get { return (String)GenreProperty.GetValue(); }
      set { GenreProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or Sets the Start time.
    /// </summary>
    public DateTime StartTime
    {
      get { return (DateTime)StartTimeProperty.GetValue(); }
      set { StartTimeProperty.SetValue(value); UpdateDuration(); }
    }

    /// <summary>
    /// Gets or Sets the End time.
    /// </summary>
    public DateTime EndTime
    {
      get { return (DateTime)EndTimeProperty.GetValue(); }
      set { EndTimeProperty.SetValue(value); UpdateDuration(); }
    }

    /// <summary>
    /// Gets or Sets the remaining duration. The value gets calculated from the difference of "EndTime - StartTime".
    /// If Start is less DateTime.Now, "EndTime - DateTime.Now" is used instead.
    /// </summary>
    public int RemainingDuration
    {
      get { return (int)RemainingDurationProperty.GetValue(); }
      set { RemainingDurationProperty.SetValue(value); }
    }

    public ProgramProperties() : this(DateTime.Now, DateTime.Now.AddHours(SlimTvMultiChannelGuideModel.VISIBLE_HOURS))
    {
    }

    public ProgramProperties(DateTime viewPortMinTime, DateTime viewPortMaxTime)
    {
      _viewPortMinTime = viewPortMinTime;
      _viewPortMaxTime = viewPortMaxTime;
      TitleProperty = new WProperty(typeof(String), String.Empty);
      DescriptionProperty = new WProperty(typeof(String), String.Empty);
      GenreProperty = new WProperty(typeof(String), String.Empty);
      StartTimeProperty = new WProperty(typeof(DateTime), DateTime.MinValue);
      EndTimeProperty = new WProperty(typeof(DateTime), DateTime.MinValue);
      RemainingDurationProperty = new WProperty(typeof(int), 0);
    }

    public void SetProgram(IProgram program)
    {
      if (program != null)
      {
        Title = program.Title;
        Description = program.Description;
        StartTime = program.StartTime;
        EndTime = program.EndTime;
        Genre = program.Genre;
        UpdateDuration();
      }
    }

    public void UpdateDuration(DateTime viewPortMinTime, DateTime viewPortMaxTime)
    {
      _viewPortMinTime = viewPortMinTime;
      _viewPortMaxTime = viewPortMaxTime;
      UpdateDuration();
    }

    private void UpdateDuration()
    {
      DateTime programStart = StartTime;
      if (programStart < _viewPortMinTime)
        programStart = _viewPortMinTime;

      DateTime programEnd = EndTime;
      if (programEnd > _viewPortMaxTime)
        programEnd = _viewPortMaxTime;

      RemainingDuration = Math.Max((int)(programEnd - programStart).TotalMinutes, 0);
    }
  }
}