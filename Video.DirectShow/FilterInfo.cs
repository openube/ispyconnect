// AForge Direct Show Library
// AForge.NET framework
//
// Copyright © Andrew Kirillov, 2008
// andrew.kirillov@gmail.com
//

namespace AForge.Video.DirectShow
{
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;
    using AForge.Video.DirectShow.Internals;

    /// <summary>
    /// DirectShow filter information.
    /// </summary>
    /// 
    public class FilterInfo : IComparable
    {
        /// <summary>
        /// Filter name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Filters's moniker string.
        /// </summary>
        /// 
        public string MonikerString { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterInfo"/> class.
        /// </summary>
        /// 
        /// <param name="monikerString">Filters's moniker string.</param>
        /// 
        public FilterInfo( string monikerString )
        {
            MonikerString = monikerString;
            Name = GetName( monikerString );
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterInfo"/> class.
        /// </summary>
        /// 
        /// <param name="moniker">Filter's moniker object.</param>
        /// 
        internal FilterInfo( IMoniker moniker )
        {
            MonikerString = GetMonikerString( moniker );
            Name = GetName( moniker );
        }

        /// <summary>
        /// Compare the object with another instance of this class.
        /// </summary>
        /// 
        /// <param name="value">Object to compare with.</param>
        /// 
        /// <returns>A signed number indicating the relative values of this instance and <b>value</b>.</returns>
        /// 
        public int CompareTo( object value )
        {
            FilterInfo f = (FilterInfo) value;

            if ( f == null )
                return 1;

            return ( this.Name.CompareTo( f.Name ) );
        }

        /// <summary>
        /// Create an instance of the filter.
        /// </summary>
        /// 
        /// <param name="filterMoniker">Filter's moniker string.</param>
        /// 
        /// <returns>Returns filter's object, which implements <b>IBaseFilter</b> interface.</returns>
        /// 
        /// <remarks>The returned filter's object should be released using <b>Marshal.ReleaseComObject()</b>.</remarks>
        /// 
        public static object CreateFilter( string filterMoniker )
        {
            object filterObject = null;
            object comObj = null;
            ICreateDevEnum enumDev = null;
            IEnumMoniker enumMon = null;
            IMoniker[] devMon = new IMoniker[1];
            int hr;
            
            try
            {
                // Get the system device enumerator
                Type srvType = Type.GetTypeFromCLSID(Clsid.SystemDeviceEnum);
                if (srvType == null)
                    throw new ApplicationException("Failed creating device enumerator");

                // create device enumerator
                comObj = Activator.CreateInstance(srvType);
                enumDev = (ICreateDevEnum)comObj;

                // Create an enumerator to find filters of specified category

                Guid cat = FilterCategory.VideoInputDevice;
                hr = enumDev.CreateClassEnumerator(ref cat, out enumMon, 0);
                if (hr != 0)
                    throw new ApplicationException("No devices of the category");

                // Collect all filters
                IntPtr n = IntPtr.Zero;
                while (true)
                {
                    // Get next filter
                    hr = enumMon.Next(1, devMon, n);
                    if ((hr != 0) || (devMon[0] == null))
                        break;

                    // Add the filter
                    FilterInfo filter = new FilterInfo(devMon[0]);
                    if (filter.MonikerString==filterMoniker)
                    {
                        Guid filterId = typeof(IBaseFilter).GUID;
                        devMon[0].BindToObject( null, null, ref filterId, out filterObject );
                        break;
                    }

                    // Release COM object
                    Marshal.ReleaseComObject(devMon[0]);
                    devMon[0] = null;
                }
            }
            catch
            {
            }
            finally
            {
                // release all COM objects
                enumDev = null;
                if (comObj != null)
                {
                    Marshal.ReleaseComObject(comObj);
                    comObj = null;
                }
                if (enumMon != null)
                {
                    Marshal.ReleaseComObject(enumMon);
                    enumMon = null;
                }
                if (devMon[0] != null)
                {
                    Marshal.ReleaseComObject(devMon[0]);
                    devMon[0] = null;
                }
            }
            return filterObject;
            //// filter's object
            //object filterObject = null;
            //// bind context and moniker objects
            //IBindCtx bindCtx = null;
            //IMoniker moniker = null;

            //int n = 0;

            //// create bind context
            //if ( Win32.CreateBindCtx( 0, out bindCtx ) == 0 )
            //{
            //    // convert moniker`s string to a moniker
            //    if ( Win32.MkParseDisplayName( bindCtx, filterMoniker, ref n, out moniker ) == 0 )
            //    {
            //        // get device base filter
            //        Guid filterId = typeof( IBaseFilter ).GUID;
            //        moniker.BindToObject( null, null, ref filterId, out filterObject );

            //        Marshal.ReleaseComObject( moniker );
            //    }
            //    Marshal.ReleaseComObject( bindCtx );
            //}
            //return filterObject;
        }

        //
        // Get moniker string of the moniker
        //
        private string GetMonikerString( IMoniker moniker )
        {
            string str;
            moniker.GetDisplayName( null, null, out str );
            return str;
        }

        //
        // Get filter name represented by the moniker
        //
        private string GetName( IMoniker moniker )
        {
            Object bagObj = null;
            IPropertyBag bag = null;

            try
            {
                Guid bagId = typeof( IPropertyBag ).GUID;
                // get property bag of the moniker
                moniker.BindToStorage( null, null, ref bagId, out bagObj );
                bag = (IPropertyBag) bagObj;

                // read FriendlyName
                object val = "";
                int hr = bag.Read( "FriendlyName", ref val, IntPtr.Zero );
                if ( hr != 0 )
                    Marshal.ThrowExceptionForHR( hr );

                // get it as string
                string ret = (string) val;
                if ( ( ret == null ) || ( ret.Length < 1 ) )
                    throw new ApplicationException( );

                return ret;
            }
            catch ( Exception )
            {
                return "";
            }
            finally
            {
                // release all COM objects
                bag = null;
                if ( bagObj != null )
                {
                    Marshal.ReleaseComObject( bagObj );
                    bagObj = null;
                }
            }
        }

        //
        // Get filter name represented by the moniker string
        //
        private string GetName( string monikerString )
        {
            IBindCtx bindCtx = null;
            IMoniker moniker = null;
            String name = "";
            int n = 0;

            // create bind context
            if ( Win32.CreateBindCtx( 0, out bindCtx ) == 0 )
            {
                // convert moniker`s string to a moniker
                if ( Win32.MkParseDisplayName( bindCtx, monikerString, ref n, out moniker ) == 0 )
                {
                    // get device name
                    name = GetName( moniker );

                    Marshal.ReleaseComObject( moniker );
                    moniker = null;
                }
                Marshal.ReleaseComObject( bindCtx );
                bindCtx = null;
            }
            return name;
        }
    }
}
