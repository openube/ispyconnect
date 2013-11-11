using System;
using NetFwTypeLib;

namespace Moah
{
    class WinXPSP2FireWall
    {
        public enum FW_ERROR_CODE
        {
            FW_NOERROR = 0,
            FW_ERR_INITIALIZED,					// Already initialized or doesn't call Initialize()
            FW_ERR_CREATE_SETTING_MANAGER,		// Can't create an instance of the firewall settings manager
            FW_ERR_LOCAL_POLICY,				// Can't get local firewall policy
            FW_ERR_PROFILE,						// Can't get the firewall profile
            FW_ERR_FIREWALL_IS_ENABLED,			// Can't get the firewall enable information
            FW_ERR_FIREWALL_ENABLED,			// Can't set the firewall enable option
            FW_ERR_INVALID_ARG,					// Invalid Arguments
            FW_ERR_AUTH_APPLICATIONS,			// Failed to get authorized application list
            FW_ERR_APP_ENABLED,					// Failed to get the application is enabled or not
            FW_ERR_CREATE_APP_INSTANCE,			// Failed to create an instance of an authorized application
            FW_ERR_SYS_ALLOC_STRING,			// Failed to alloc a memory for BSTR
            FW_ERR_PUT_PROCESS_IMAGE_NAME,		// Failed to put Process Image File Name to Authorized Application
            FW_ERR_PUT_REGISTER_NAME,			// Failed to put a registered name
            FW_ERR_ADD_TO_COLLECTION,			// Failed to add to the Firewall collection
            FW_ERR_REMOVE_FROM_COLLECTION,		// Failed to remove from the Firewall collection
            FW_ERR_GLOBAL_OPEN_PORTS,			// Failed to retrieve the globally open ports
            FW_ERR_PORT_IS_ENABLED,				// Can't get the firewall port enable information
            FW_ERR_PORT_ENABLED,				// Can't set the firewall port enable option
            FW_ERR_CREATE_PORT_INSTANCE,		// Failed to create an instance of an authorized port
            FW_ERR_SET_PORT_NUMBER,				// Failed to set port number
            FW_ERR_SET_IP_PROTOCOL,				// Failed to set IP Protocol
            FW_ERR_EXCEPTION_NOT_ALLOWED,		// Failed to get or put the exception not allowed
            FW_ERR_NOTIFICATION_DISABLED,		// Failed to get or put the notification disabled
            FW_ERR_UNICAST_MULTICAST,			// Failed to get or put the UnicastResponses To MulticastBroadcast Disabled Property 
            FW_ERR_APPLICATION_ITEM,            // Failed to returns the specified application if it is in the collection.
            FW_ERR_SAME_PORT_EXIST,             // The port which you try to add is already existed.
            FW_ERR_UNKNOWN,                     // Unknown Error or Exception occured
        };

        INetFwProfile m_FirewallProfile = null;

        public FW_ERROR_CODE Initialize()
        {
            if (m_FirewallProfile != null)
                return FW_ERROR_CODE.FW_ERR_INITIALIZED;

            Type typFwMgr;
            INetFwMgr fwMgr;

            typFwMgr = Type.GetTypeFromCLSID(new Guid("{304CE942-6E39-40D8-943A-B913C40C9CD4}"));
            fwMgr = (INetFwMgr)Activator.CreateInstance(typFwMgr);

            if (fwMgr == null)
                return FW_ERROR_CODE.FW_ERR_CREATE_SETTING_MANAGER;
            INetFwPolicy fwPolicy = fwMgr.LocalPolicy;
            if (fwPolicy == null)
                return FW_ERROR_CODE.FW_ERR_LOCAL_POLICY;

            try
            {
                m_FirewallProfile = fwPolicy.GetProfileByType(fwMgr.CurrentProfileType);
            }
            catch
            {
                return FW_ERROR_CODE.FW_ERR_PROFILE;
            }

            return FW_ERROR_CODE.FW_NOERROR;
        }

        public FW_ERROR_CODE Uninitialize()
        {
            m_FirewallProfile = null;
            return FW_ERROR_CODE.FW_NOERROR;
        }

        public FW_ERROR_CODE IsWindowsFirewallOn(ref bool bOn)
        {
            bOn = false;

            if (m_FirewallProfile == null)
                return FW_ERROR_CODE.FW_ERR_INITIALIZED;

            bOn = m_FirewallProfile.FirewallEnabled;

            return FW_ERROR_CODE.FW_NOERROR;
        }

        public FW_ERROR_CODE TurnOnWindowsFirewall()
        {
            if (m_FirewallProfile == null)
                return FW_ERROR_CODE.FW_ERR_INITIALIZED;

            // Check whether the firewall is off
            bool bFWOn = false;
            FW_ERROR_CODE ret = IsWindowsFirewallOn(ref bFWOn);
            if (ret != FW_ERROR_CODE.FW_NOERROR)
                return ret;

            // If it is off now, turn it on
            if (!bFWOn)
                m_FirewallProfile.FirewallEnabled = true;

            return FW_ERROR_CODE.FW_NOERROR;
        }

        public FW_ERROR_CODE TurnOffWindowsFirewall()
        {
            if (m_FirewallProfile == null)
                return FW_ERROR_CODE.FW_ERR_INITIALIZED;

            // Check whether the firewall is off
            bool bFWOn = false;
            FW_ERROR_CODE ret = IsWindowsFirewallOn(ref bFWOn);

            if (ret != FW_ERROR_CODE.FW_NOERROR)
                return ret;

            // If it is on now, turn it off
            if (bFWOn)
                m_FirewallProfile.FirewallEnabled = false;

            return FW_ERROR_CODE.FW_NOERROR;
        }

        public FW_ERROR_CODE IsAppEnabled(string strProcessImageFileName, ref bool bEnable)
        {
            if (m_FirewallProfile == null)
                return FW_ERROR_CODE.FW_ERR_INITIALIZED;

            if (strProcessImageFileName.Length == 0)
                return FW_ERROR_CODE.FW_ERR_INVALID_ARG;

            INetFwAuthorizedApplications FWApps = m_FirewallProfile.AuthorizedApplications;
            if (FWApps == null)
                return FW_ERROR_CODE.FW_ERR_AUTH_APPLICATIONS;

            try
            {
                INetFwAuthorizedApplication FWApp = FWApps.Item(strProcessImageFileName);
                // If FAILED, the appliacation is not in the collection list
                if (FWApp == null)
                    return FW_ERROR_CODE.FW_ERR_APPLICATION_ITEM;

                bEnable = FWApp.Enabled;
            }
            catch (System.IO.FileNotFoundException)
            {
                bEnable = false;
            }

            return FW_ERROR_CODE.FW_NOERROR;
        }

        public FW_ERROR_CODE AddApplication(string strProcessImageFileName, string strRegisterName)
        {
            if (m_FirewallProfile == null)
                return FW_ERROR_CODE.FW_ERR_INITIALIZED;

            if (strProcessImageFileName.Length == 0 || strRegisterName.Length == 0)
                return FW_ERROR_CODE.FW_ERR_INVALID_ARG;

            // First of all, check the application is already authorized;
            bool bAppEnable = true;
            FW_ERROR_CODE nError = IsAppEnabled(strProcessImageFileName, ref bAppEnable);
            if (nError != FW_ERROR_CODE.FW_NOERROR)
                return nError;

            // Only add the application if it isn't authorized
            if (bAppEnable == false)
            {
                // Retrieve the authorized application collection
                INetFwAuthorizedApplications FWApps = m_FirewallProfile.AuthorizedApplications;

                if (FWApps == null)
                    return FW_ERROR_CODE.FW_ERR_AUTH_APPLICATIONS;

                // Create an instance of an authorized application
                Type typeFwApp = Type.GetTypeFromCLSID(new Guid("{EC9846B3-2762-4A6B-A214-6ACB603462D2}"));

                INetFwAuthorizedApplication FWApp = (INetFwAuthorizedApplication)Activator.CreateInstance(typeFwApp);
                if (FWApp == null)
                    return FW_ERROR_CODE.FW_ERR_CREATE_APP_INSTANCE;

                // Set the process image file name
                FWApp.ProcessImageFileName = strProcessImageFileName;
                FWApp.Name = strRegisterName;

                try
                {
                    FWApps.Add(FWApp);
                }
                catch
                {
                    return FW_ERROR_CODE.FW_ERR_ADD_TO_COLLECTION;
                }

            }

            return FW_ERROR_CODE.FW_NOERROR;
        }

        public FW_ERROR_CODE RemoveApplication(string strProcessImageFileName)
        {
            if (m_FirewallProfile == null)
                return FW_ERROR_CODE.FW_ERR_INITIALIZED;
            if (strProcessImageFileName.Length == 0)
                return FW_ERROR_CODE.FW_ERR_INVALID_ARG;

            bool bAppEnable = true;
            FW_ERROR_CODE nError = IsAppEnabled(strProcessImageFileName, ref bAppEnable);

            if (nError != FW_ERROR_CODE.FW_NOERROR)
                return nError;

            // Only remove the application if it is authorized
            if (bAppEnable)
            {
                // Retrieve the authorized application collection
                INetFwAuthorizedApplications FWApps = m_FirewallProfile.AuthorizedApplications;
                if (FWApps == null)
                    return FW_ERROR_CODE.FW_ERR_AUTH_APPLICATIONS;

                try
                {
                    FWApps.Remove(strProcessImageFileName);
                }
                catch
                {
                    return FW_ERROR_CODE.FW_ERR_REMOVE_FROM_COLLECTION;
                }
            }

            return FW_ERROR_CODE.FW_NOERROR;
        }

        public FW_ERROR_CODE IsPortEnabled(int nPortNumber, NET_FW_IP_PROTOCOL_ ipProtocol, ref bool bEnable)
        {
            if (m_FirewallProfile == null)
                return FW_ERROR_CODE.FW_ERR_INITIALIZED;

            // Retrieve the open ports collection
            INetFwOpenPorts FWOpenPorts = m_FirewallProfile.GloballyOpenPorts;
            if (FWOpenPorts == null)
                return FW_ERROR_CODE.FW_ERR_GLOBAL_OPEN_PORTS;

            // Get the open port
            try
            {
                INetFwOpenPort FWOpenPort = FWOpenPorts.Item(nPortNumber, ipProtocol);
                if (FWOpenPort != null)
                    bEnable = FWOpenPort.Enabled;
                else
                    bEnable = false;
            }
            catch (System.IO.FileNotFoundException)
            {
                bEnable = false;
            }

            return FW_ERROR_CODE.FW_NOERROR;
        }

        public FW_ERROR_CODE AddPort(int nPortNumber, NET_FW_IP_PROTOCOL_ ipProtocol, string strRegisterName)
        {
            if (m_FirewallProfile == null)
                return FW_ERROR_CODE.FW_ERR_INITIALIZED;

            bool bEnablePort = true;
            FW_ERROR_CODE nError = IsPortEnabled(nPortNumber, ipProtocol, ref bEnablePort);
            if (nError != FW_ERROR_CODE.FW_NOERROR)
                return nError;

            // Only add the port, if it isn't added to the collection
            if (bEnablePort == false)
            {
                // Retrieve the collection of globally open ports
                INetFwOpenPorts FWOpenPorts = m_FirewallProfile.GloballyOpenPorts;
                if (FWOpenPorts == null)
                    return FW_ERROR_CODE.FW_ERR_GLOBAL_OPEN_PORTS;

                // Create an instance of an open port
                Type typeFwPort = Type.GetTypeFromCLSID(new Guid("{0CA545C6-37AD-4A6C-BF92-9F7610067EF5}"));
                INetFwOpenPort FWOpenPort = (INetFwOpenPort)Activator.CreateInstance(typeFwPort);
                if (FWOpenPort == null)
                    return FW_ERROR_CODE.FW_ERR_CREATE_PORT_INSTANCE;

                // Set the port number
                FWOpenPort.Port = nPortNumber;

                // Set the IP Protocol
                FWOpenPort.Protocol = ipProtocol;

                // Set the registered name
                FWOpenPort.Name = strRegisterName;

                try
                {
                    FWOpenPorts.Add(FWOpenPort);
                }
                catch
                {
                    return FW_ERROR_CODE.FW_ERR_ADD_TO_COLLECTION;
                }
            }
            else
                return FW_ERROR_CODE.FW_ERR_SAME_PORT_EXIST;

            return FW_ERROR_CODE.FW_NOERROR;
        }

        public FW_ERROR_CODE RemovePort(int nPortNumber, NET_FW_IP_PROTOCOL_ ipProtocol)
        {
            if (m_FirewallProfile == null)
                return FW_ERROR_CODE.FW_ERR_INITIALIZED;

            bool bEnablePort = false;
            FW_ERROR_CODE nError = IsPortEnabled(nPortNumber, ipProtocol, ref bEnablePort);
            if (nError != FW_ERROR_CODE.FW_NOERROR)
                return nError;

            // Only remove the port, if it is on the collection
            if (bEnablePort)
            {
                // Retrieve the collection of globally open ports
                INetFwOpenPorts FWOpenPorts = m_FirewallProfile.GloballyOpenPorts;
                if (FWOpenPorts == null)
                    return FW_ERROR_CODE.FW_ERR_GLOBAL_OPEN_PORTS;

                try
                {
                    FWOpenPorts.Remove(nPortNumber, ipProtocol);
                }
                catch
                {
                    return FW_ERROR_CODE.FW_ERR_REMOVE_FROM_COLLECTION;
                }
            }

            return FW_ERROR_CODE.FW_NOERROR;
        }

        public FW_ERROR_CODE IsExceptionNotAllowed(ref bool bNotAllowed)
        {
            if (m_FirewallProfile == null)
                return FW_ERROR_CODE.FW_ERR_INITIALIZED;
            bNotAllowed = m_FirewallProfile.ExceptionsNotAllowed;

            return FW_ERROR_CODE.FW_NOERROR;
        }

        public FW_ERROR_CODE SetExceptionNotAllowed(bool bNotAllowed)
        {
            if (m_FirewallProfile == null)
                return FW_ERROR_CODE.FW_ERR_INITIALIZED;

            m_FirewallProfile.ExceptionsNotAllowed = bNotAllowed;

            return FW_ERROR_CODE.FW_NOERROR;
        }

        public FW_ERROR_CODE IsNotificationDiabled(ref bool bDisabled)
        {
            if (m_FirewallProfile == null)
                return FW_ERROR_CODE.FW_ERR_INITIALIZED;

            bDisabled = m_FirewallProfile.NotificationsDisabled;

            return FW_ERROR_CODE.FW_NOERROR;
        }

        public FW_ERROR_CODE SetNotificationDiabled(bool bDisabled)
        {
            if (m_FirewallProfile == null)
                return FW_ERROR_CODE.FW_ERR_INITIALIZED;

            m_FirewallProfile.NotificationsDisabled = bDisabled;

            return FW_ERROR_CODE.FW_NOERROR;
        }

        public FW_ERROR_CODE IsUnicastResponsesToMulticastBroadcastDisabled(ref bool bDisabled)
        {
            if (m_FirewallProfile == null)
                return FW_ERROR_CODE.FW_ERR_INITIALIZED;

            bDisabled = m_FirewallProfile.UnicastResponsesToMulticastBroadcastDisabled;

            return FW_ERROR_CODE.FW_NOERROR;
        }

        public FW_ERROR_CODE SetUnicastResponsesToMulticastBroadcastDisabled(bool bDisabled)
        {
            if (m_FirewallProfile == null)
                return FW_ERROR_CODE.FW_ERR_INITIALIZED;

            m_FirewallProfile.UnicastResponsesToMulticastBroadcastDisabled = bDisabled;

            return FW_ERROR_CODE.FW_NOERROR;
        }
    }
}
