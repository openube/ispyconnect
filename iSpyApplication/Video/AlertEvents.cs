using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iSpyApplication.Video
{
    public delegate void AlertEventHandler(object sender, AlertEventArgs eventArgs);

    /// <summary>
    /// Arguments for Audio source error event from Audio source.
    /// </summary>
    /// 
    public class AlertEventArgs : EventArgs
    {
        private string description;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlertEventArgs"/> class.
        /// </summary>
        /// 
        /// <param name="description">Error description.</param>
        /// 
        public AlertEventArgs(string description)
        {
            this.description = description;
        }

        /// <summary>
        /// Audio source error description.
        /// </summary>
        /// 
        public string Description
        {
            get { return description; }
        }
    }
}
