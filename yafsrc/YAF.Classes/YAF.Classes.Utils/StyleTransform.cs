﻿/* Yet Another Forum.net
 * Copyright (C) 2006-2009 Jaben Cargman
 * http://www.yetanotherforum.net/
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 */
namespace YAF.Classes.Utils
{
  #region Using

  using System;
  using System.Data;
  using Interfaces;

  #endregion

  /* Created by vzrus(c) for Yet Another Forum and can be use with any Yet Another Forum licence and modified in any way.*/

  /// <summary>
  /// Transforms the style.
  /// </summary>
  public class StyleTransform : IStyleTransform
  {
    private IYafTheme _theme;

    public string CurrentThemeFile
    {
      get
      {
        if (_theme != null)
        {
          return _theme.ThemeFile.ToLower().Trim();
        }

        return string.Empty;
      }
    }

    public StyleTransform(IYafTheme theme)
    {
      _theme = theme;
    }

    /// <summary>
    /// The decode style by table.
    /// </summary>
    /// <param name="dt">
    /// The dt.
    /// </param>
    public void DecodeStyleByTable(ref DataTable dt)
    {
      DecodeStyleByTable(ref dt, false);
    }

    /// <summary>
    /// The decode style by table.
    /// </summary>
    /// <param name="dt">
    /// The dt.
    /// </param>
    /// <param name="colorOnly">
    /// The color only.
    /// </param>
    public void DecodeStyleByTable(ref DataTable dt, bool colorOnly)
    {
      DecodeStyleByTable(ref dt, colorOnly, "Style");
    }

    /// <summary>
    /// The decode style by table.
    /// </summary>
    /// <param name="dt">
    /// The dt.
    /// </param>
    /// <param name="colorOnly">
    /// The color only.
    /// </param>
    /// <param name="styleColumns">
    /// The style Columns.
    /// </param>
    /// <param name="colorOnly">
    /// The styleColumns can contain param array to handle several style columns.
    /// </param>
    public void DecodeStyleByTable(ref DataTable dt, bool colorOnly, params string[] styleColumns)
    {
      for (int i = 0; i < dt.Rows.Count; i++)
      {
        for (int k = 0; k < styleColumns.Length; k++)
        {
          DataRow dr = dt.Rows[i];
          DecodeStyleByRow(ref dr, styleColumns[k], colorOnly);
        }
      }
    }

    /// <summary>
    /// The decode style by row.
    /// </summary>
    /// <param name="dr">
    /// The dr.
    /// </param>
    public void DecodeStyleByRow(ref DataRow dr)
    {
      DecodeStyleByRow(ref dr, false);
    }

    /// <summary>
    /// The decode style by row.
    /// </summary>
    /// <param name="dr">
    /// The dr.
    /// </param>
    /// <param name="colorOnly">
    /// The color only.
    /// </param>
    public void DecodeStyleByRow(ref DataRow dr, bool colorOnly)
    {
      DecodeStyleByRow(ref dr, "Style", colorOnly);
    }

    /// <summary>
    /// The decode style by row.
    /// </summary>
    /// <param name="dr">
    /// The dr.
    /// </param>
    /// <param name="columnName">the style column name</param>
    /// <param name="colorOnly">
    /// The color only.
    /// </param>
    public void DecodeStyleByRow(ref DataRow dr, string columnName, bool colorOnly)
    {
      dr[columnName] = DecodeStyleByString(dr[columnName].ToString(), colorOnly);
    }

    /// <summary>
    /// The decode style by string.
    /// </summary>
    /// <param name="styleStr">
    /// The style str.
    /// </param>
    /// <param name="colorOnly">
    /// The color only.
    /// </param>
    /// <returns>
    /// The decode style by string.
    /// </returns>
    public string DecodeStyleByString(string styleStr, bool colorOnly)
    {
      string[] styleRow = styleStr.Trim().Split('/');
      for (int i = 0; i < styleRow.GetLength(0); i++)
      {
        string[] pair = styleRow[i].Split('!');
        if (pair[0].ToLowerInvariant().Trim() == "default")
        {
          if (colorOnly)
          {
            styleStr = GetColorOnly(pair[1]);
          }
          else
          {
            styleStr = pair[1];
          }
        }

        styleStr = DecodeStyleByString(styleStr, colorOnly, pair);
      }

      return styleStr;
    }

    private string DecodeStyleByString(string styleStr, bool colorOnly, string[] pair)
    {
      string styleStrResult = styleStr;

      for (int j = 0; j < pair.Length; j++)
      {
        string filename = pair[0] + ".xml";

        if (filename.Trim().Equals(CurrentThemeFile, StringComparison.CurrentCultureIgnoreCase))
        {
          styleStrResult = colorOnly ? GetColorOnly(pair[1]) : pair[1];
        }
      }

      return styleStrResult;
    }

    /// <summary>
    /// The get color only.
    /// </summary>
    /// <param name="styleString">
    /// The style string.
    /// </param>
    /// <returns>
    /// The get color only.
    /// </returns>
    private string GetColorOnly(string styleString)
    {
      string[] styleArray = styleString.Split(';');
      for (int i = 0; i < styleArray.Length; i++)
      {
        if (styleArray[i].ToLower().Contains("color"))
        {
          return styleArray[i];
        }
      }

      return null;
    }
  }
}