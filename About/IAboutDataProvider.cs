// ======================================================================
// ROM RESIGNER
// Copyright (C) 2013 Ilya Egorov (goldrenard@gmail.com)

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.
// ======================================================================

using System;

namespace ROMResigner.About
{
    /// <summary>
    /// This interface describes the data that's used to populate the WpfAboutBox.
    /// The properties correspond to fields that are shown in the About dialog. 
    /// Multiple providers can implement this interface to surface this data differently 
    /// to the About dialog.
    /// </summary>
    public interface IAboutDataProvider
    {
        /// <summary>
        /// Gets the title property, which is display in the About dialogs window title.
        /// </summary>
        string Title
        {
            get;
        }

        /// <summary>
        /// Gets the application's version information to show.
        /// </summary>
        string Version
        {
            get;
        }

        /// <summary>
        /// Gets the description about the application.
        /// </summary>
        string Description
        {
            get;
        }

        /// <summary>
        ///  Gets the product's full name.
        /// </summary>
        string Product
        {
            get;
        }

        /// <summary>
        /// Gets the copyright information for the product.
        /// </summary>
        string Copyright
        {
            get;
        }

        /// <summary>
        /// Gets the product's company name.
        /// </summary>
        string Company
        {
            get;
        }

        /// <summary>
        /// Gets the link text to display in the About dialog.
        /// </summary>
        string LinkText
        {
            get;
        }

        /// <summary>
        /// Gets the link uri that is the navigation target of the link.
        /// </summary>
        string LinkUri
        {
            get;
        }
    }
}
