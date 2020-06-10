// Copyright 2018 ESRI
// 
// All rights reserved under the copyright laws of the United States
// and applicable international laws, treaties, and conventions.
// 
// You may freely redistribute and use this sample code, with or
// without modification, provided you include the original copyright
// notice and use restrictions.
// 
// See the use restrictions at <your Enterprise SDK install location>/userestrictions.txt.
// 

using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Server;
using ESRI.ArcGIS.SOESupport;
using SOE_Utilita;
using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

//TODO: sign the project (project properties > signing tab > sign the assembly)
//      this is strongly suggested if the dll will be registered using regasm.exe <your>.dll /codebase


namespace SOE_FeatureDownload
{
    [ComVisible(true)]
    [Guid("d186f6b3-2bec-4b59-8110-5dfad87bca63")]
    [ClassInterface(ClassInterfaceType.None)]
    [ServerObjectExtension("MapServer",//use "MapServer" if SOE extends a Map service and "ImageServer" if it extends an Image service.
        AllCapabilities = "",
        DefaultCapabilities = "",
        Description = "",
        DisplayName = "SOE_FeatureDownload",
        Properties = "",
        SupportsREST = true,
        SupportsSOAP = false)]
    public class SOE_FeatureDownload : IServerObjectExtension, IObjectConstruct, IRESTRequestHandler
    {
        private string soe_name;

        private IPropertySet configProps;
        private IServerObjectHelper serverObjectHelper;
        private ServerLogger logger;
        private IRESTRequestHandler reqHandler;

        public SOE_FeatureDownload()
        {
            soe_name = this.GetType().Name;
            logger = new ServerLogger();
            reqHandler = new SoeRestImpl(soe_name, CreateRestSchema()) as IRESTRequestHandler;
        }

        #region IServerObjectExtension Members

        public void Init(IServerObjectHelper pSOH)
        {
            serverObjectHelper = pSOH;
        }

        public void Shutdown()
        {
        }

        #endregion

        #region IObjectConstruct Members

        public void Construct(IPropertySet props)
        {
            configProps = props;
        }

        #endregion

        #region IRESTRequestHandler Members

        public string GetSchema()
        {
            return reqHandler.GetSchema();
        }

        public byte[] HandleRESTRequest(string Capabilities, string resourceName, string operationName, string operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            return reqHandler.HandleRESTRequest(Capabilities, resourceName, operationName, operationInput, outputFormat, requestProperties, out responseProperties);
        }

        #endregion

        private RestResource CreateRestSchema()
        {
            RestResource rootRes = new RestResource(soe_name, false, RootResHandler);

            RestOperation operazioneRestDownloadFeature = new RestOperation("oggettiDownload",
                                                      new string[] { "listaOID", "URLServiceLayer" },
                                                      new string[] { "json" },
                                                      OperazioneRestDownloadFeatureHandler);

            rootRes.operations.Add(operazioneRestDownloadFeature);

            return rootRes;
        }

        private byte[] RootResHandler(NameValueCollection boundVariables, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = null;

            JsonObject result = new JsonObject();

            return Encoding.UTF8.GetBytes(result.ToJson());
        }

        private byte[] OperazioneRestDownloadFeatureHandler(NameValueCollection boundVariables,
                                                  JsonObject operationInput,
                                                      string outputFormat,
                                                      string requestProperties,
                                                  out string responseProperties)
        {
            responseProperties = null;

            #region Istanzio il JSON Result
            JsonObject result = new JsonObject();
            result.AddBoolean("hasError", false);
            #endregion

            bool found = operationInput.TryGetArray("listaOID", out object[] paramListaOID);
            if (!found || paramListaOID == null)
                throw new ArgumentNullException("listaOID");

            bool okParam2 = operationInput.TryGetString("URLServiceLayer", out string paramURL);
            if (!okParam2 || paramURL == String.Empty)
                throw new ArgumentNullException("URLServiceLayer");

            try
            {
                result.AddArray("interno", paramListaOID);

                // Ricavo Feature Class dietro al Service Layer
                IFeatureClass featureClass = MapServiceHelper_GiancaGIS.RicavaFCDaURLServiceLayer(this.serverObjectHelper, paramURL);

                IWorkspace workspace = WorkspaceHelper_GiancaGIS.RicavaWorkSpaceFC(featureClass, out IPropertySet propertySetIN, 
                    out string tipoWorkspace, out bool errore, out string msgErrore);

                this.CreaWorkSpaceOutput(out IPropertySet propertySetOUT, out string pathFGDB);

                SOE_Utilita.WorkspaceHelper_GiancaGIS.CopiaFeatureClass(propertySetIN, propertySetOUT, (featureClass as IDataset).Name, out string warning);
            
            }
            catch (Exception errore)
            {
                result.AddString("errorDescription", errore.Message);
                result.AddBoolean("hasError", true);
            }

            return Encoding.UTF8.GetBytes(result.ToJson());
        }

        private void CreaWorkSpaceOutput(out IPropertySet propertySet, out string pathFGDB)
        {
            IWorkspaceFactory2 workF = new FileGDBWorkspaceFactory() as IWorkspaceFactory2;
            Guid g = Guid.NewGuid();

            string timeStamp = DateTime.UtcNow.ToString("dd-MM-yyyy", CultureInfo.CurrentUICulture);

            string basePath = $@"C:\arcgisserver\directories\arcgisoutput\_ags_{g}";

            string nomeFGDB = $@"FGDB_{timeStamp}.gdb";

            pathFGDB = System.IO.Path.Combine(basePath, nomeFGDB);

            if (!System.IO.Directory.Exists(pathFGDB))
                System.IO.Directory.CreateDirectory(pathFGDB);

            #region Parte dedicata alla creazione di un File Geodatabase
            // Istanzio l'oggetto singleton
            IWorkspaceFactory2 workspaceFactory = new FileGDBWorkspaceFactory() as IWorkspaceFactory2;
               
            workspaceFactory.Create(pathFGDB, nomeFGDB, null, 0);

            #endregion

            // Mi dedico nella creazione del FGDB locale
            propertySet = new PropertySetClass();
            propertySet.SetProperty("DATABASE", pathFGDB);

            #region Rilascio tutti gli oggetti singleton
            int resLeft = 0;
            do
            {
                resLeft = System.Runtime.InteropServices.Marshal.ReleaseComObject(workspaceFactory);
            }
            while (resLeft > 0);
            #endregion

        }

    }
}
