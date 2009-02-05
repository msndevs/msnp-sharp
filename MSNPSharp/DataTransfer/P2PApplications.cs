using System;
using System.Net;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp.IO;
    using MSNPSharp.Core;
    using MSNPSharp.DataTransfer;

    #region P2PApplicationAttribute

    public class P2PApplicationAttribute : Attribute
    {
        uint appId;
        string eufGuid;

        public uint AppId
        {
            get
            {
                return appId;
            }
        }

        public string EufGuid
        {
            get
            {
                return eufGuid;
            }
        }

        public P2PApplicationAttribute(uint appID, string eufGuid)
        {
            this.appId = appID;
            this.eufGuid = eufGuid;
        }
    }
    #endregion

    public abstract class P2PBridge
    {
    }

   

    

    public static class P2PManager
    {
        private static List<P2PSession> sessions = new List<P2PSession>();
        private static List<P2PBridge> bridges = new List<P2PBridge>();


        public static IEnumerable<P2PSession> Sessions
        {
            get
            {
                return sessions;
            }
        }
    }

    public static class P2PApplications
    {
        private static List<P2PApp> p2pApps = new List<P2PApp>();
        private struct P2PApp
        {
            public UInt32 AppId;
            public Type AppType;
            public Guid EufGuid;
        }

        static P2PApplications()
        {
            try
            {
                AddApplication(Assembly.GetExecutingAssembly());
            }
            catch (Exception e)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Error loading built-in p2p applications: " + e.Message, "P2PApplications");
            }
        }

        #region Add/Find Application

        public static void AddApplication(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(P2PApplicationAttribute), false).Length > 0)
                    AddApplication(type);
            }
        }

        public static void AddApplication(Type type)
        {
            foreach (P2PApplicationAttribute att in type.GetCustomAttributes(typeof(P2PApplicationAttribute), false))
            {
                P2PApp app = new P2PApp();
                app.AppType = type;
                app.AppId = att.AppId;
                app.EufGuid = new Guid(att.EufGuid);

                p2pApps.Add(app);
            }
        }

        internal static Type GetApplication(Guid eufGuid, uint appId)
        {
            foreach (P2PApp app in p2pApps)
            {
                if (app.EufGuid == eufGuid && app.AppId == appId)
                    return app.AppType;
                else if (app.AppId == appId)
                    return app.AppType;
                else if (app.EufGuid == eufGuid)
                    return app.AppType;
            }

            return null;
        }

        internal static uint FindApplicationId(P2PApplication p2pApp)
        {
            foreach (P2PApp app in p2pApps)
            {
                if (app.AppType == p2pApp.GetType())
                    return app.AppId;
            }

            return 0;
        }

        internal static Guid FindApplicationEufGuid(P2PApplication p2pApp)
        {
            foreach (P2PApp app in p2pApps)
            {
                if (app.AppType == p2pApp.GetType())
                    return app.EufGuid;
            }

            return Guid.Empty;
        }



        #endregion




    }
};
