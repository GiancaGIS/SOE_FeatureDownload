using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Server;
using ESRI.ArcGIS.SOESupport;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SOE_Utilita
{
    /// <summary>
    /// class of Helper
    /// </summary>
    internal class GeneralHelper_GiancaGIS
    {
        /// <summary>
        /// Converte una Feature Class, opportunamente filtrata in un oggetto RecordSet
        /// </summary>
        /// <param name="featureClass">feature class input</param>
        /// <param name="queryFilter">query filter</param>
        /// <returns>return Recordset</returns>
        internal static IRecordSet2 ConvertToRecordset(IFeatureClass featureClass, IQueryFilter2 queryFilter)
        {
            IRecordSet recordSet = new RecordSetClass();
            IRecordSetInit recordSetInit = recordSet as IRecordSetInit;
            recordSetInit.SetSourceTable(featureClass as ITable, queryFilter);

            return (IRecordSet2)recordSetInit;
        }

        /// <summary>
        /// Converte una Feature Class, opportunamente filtrata in un oggetto RecordSet
        /// </summary>
        /// <param name="featureClass"></param>
        /// <param name="spatialFilter"></param>
        /// <returns></returns>
        internal static IRecordSet2 ConvertToRecordset(IFeatureClass featureClass, ISpatialFilter spatialFilter)
        {
            IRecordSet recordSet = new RecordSetClass();
            IRecordSetInit recordSetInit = recordSet as IRecordSetInit;
            recordSetInit.SetSourceTable(featureClass as ITable, spatialFilter);

            return (IRecordSet2)recordSetInit;
        }

        /// <summary>
        /// Converte una lista di IGeometry in un array di JsonObjects
        /// </summary>
        /// <param name="geometries">list of IGeometry</param>
        /// <returns>array of JsonObject</returns>
        internal static object[] GetListJsonObjects(List<IGeometry> geometries)
        {
            List<JsonObject> jsonObjects = new List<JsonObject>();
            geometries.ForEach(g =>
            {
                jsonObjects.Add(Conversion.ToJsonObject(g));
            });

            return jsonObjects.ToArray();
        }
    }

    /// <summary>
    /// Classe contenente metodi utili per i Map Service
    /// </summary>
    internal class MapServiceHelper_GiancaGIS : GeneralHelper_GiancaGIS
    {
        /// <summary>
        /// Metodo che restituisce una Feature Class dato un Server Object e il Url del Service Layer
        /// </summary>
        /// <param name="serverObjectHelper">Server Object</param>
        /// <param name="URLServiceLayer">URL Service Layer</param>
        /// <returns></returns>
        internal static IFeatureClass RicavaFCDaURLServiceLayer(IServerObjectHelper serverObjectHelper, string URLServiceLayer)
        {
            IFeatureClass fc;
            try
            {
                // Ricavo il numero posizionale del Service Layer
                int numPosizionale =  Convert.ToInt32(System.IO.Path.GetFileName(URLServiceLayer));

                IMapServer4 mapServer = (IMapServer4)serverObjectHelper.ServerObject;
                string nomeMapService = mapServer.DefaultMapName;

                // Use IMapServerDataAccess to get the data
                IMapServerDataAccess dataAccess = (IMapServerDataAccess)mapServer;

                // Accedo alla Feature Class sorgente
                fc = (IFeatureClass)dataAccess.GetDataSource(nomeMapService, numPosizionale);
            }
            catch (System.Exception)
            {
                throw;
            }

            return fc;
        }
    }

    internal class WorkspaceHelper_GiancaGIS : MapServiceHelper_GiancaGIS
    {
        internal static IWorkspace RicavaWorkSpaceFC(IFeatureClass featureClass,
            out IPropertySet propertySet, out string tipoWorkspace, out bool errore, out string msgErrore)
        {
            IWorkspace workspace = null;
            errore = false;
            msgErrore = String.Empty;
            tipoWorkspace = String.Empty;
            propertySet = null;

            try
            {
                workspace = featureClass.FeatureDataset.Workspace;
                tipoWorkspace = workspace.WorkspaceFactory.WorkspaceDescription[false];
                propertySet = workspace.ConnectionProperties;
            }
            catch (Exception ex)
            {
                errore = true;
                msgErrore = ex.Message;
            }

            return workspace;
        }

        internal static void CopiaFeatureClass
            (IPropertySet propertySetIN, IPropertySet propertySetOUT,
            string nomeFCIN,
            out string warningErroriCopiaDati,
            string WorkspaceFactoryProgID_IN = "esriDataSourcesGDB.SDEWorkspaceFactory",
            string WorkspaceFactoryProgID_OUT = "esriDataSourcesGDB.FileGDBWorkspaceFactor")
        {
            try
            {
                warningErroriCopiaDati = String.Empty;

                // Imposto la Workspace di output (il FGDB)
                IWorkspaceName outWorkspaceName = new WorkspaceName() as IWorkspaceName;
                outWorkspaceName.ConnectionProperties = propertySetOUT;
                //outWorkspaceName.WorkspaceFactoryProgID = WorkspaceFactoryProgID_OUT;

                IFeatureClassName OutFCName = new FeatureClassNameClass();

                IDatasetName outDatasetName = OutFCName as IDatasetName;
                outDatasetName.Name = nomeFCIN;
                outDatasetName.WorkspaceName = outWorkspaceName;
                IFeatureDatasetName outFeatureDatasetName = new FeatureDatasetNameClass();


                // Imposto la Workspace di input
                IWorkspaceFactory2 InworkF = new FileGDBWorkspaceFactory() as IWorkspaceFactory2;
                IWorkspace InWorkspace = InworkF.Open(propertySetIN, 0);
                IFeatureWorkspace featureWorkspace = InWorkspace as IFeatureWorkspace;
                
                IWorkspaceName inWorkspaceName = new WorkspaceName() as IWorkspaceName;
                inWorkspaceName.ConnectionProperties = InWorkspace.ConnectionProperties;
                //inWorkspaceName.WorkspaceFactoryProgID = WorkspaceFactoryProgID_IN;

                IFeatureClassName InFCName = new FeatureClassNameClass();

                IDatasetName InDatasetName = InFCName as IDatasetName;
                InDatasetName.Name = nomeFCIN;
                InDatasetName.WorkspaceName = inWorkspaceName;


                // Mi dedico alla apertura della Feature Class di input per ricavare le field definitions
                IFeatureClass featureClassIn = featureWorkspace.OpenFeatureClass(nomeFCIN);

                // Valido gli attributi della Feature Class di Input
                IFields fieldsIn = featureClassIn.Fields;
                IFieldChecker fieldChecker = new FieldCheckerClass();
                fieldChecker.Validate(fieldsIn, out IEnumFieldError enumFieldError, out IFields fieldsOut);

                // Loop attraverso gli attributi per cercare quello geometrico
                IField attributoGeometrico = null;
                for (int cont = 0; cont < fieldsOut.FieldCount; cont ++)
                {
                    if (fieldsOut.Field[cont].Type == esriFieldType.esriFieldTypeGeometry)
                    {
                        attributoGeometrico = fieldsOut.Field[cont];
                        break;
                    }
                }

                // Ricavo la geometry definition della Feature Class di input
                IGeometryDef geometryDefOUT_Fc = attributoGeometrico.GeometryDef;

                // Per la Feature Class di output, fornisco la geometry definition di output, lo spatial index grid count
                // e la grid size
                IGeometryDefEdit geometryDefEdit = geometryDefOUT_Fc as IGeometryDefEdit;
                geometryDefEdit.GridCount_2 = 1;
                geometryDefEdit.GridSize_2[0] = CalcolaDefaultIndexFC(featureClassIn, out bool errore, out string msgErrore);

                if (!errore)
                {
                    geometryDefEdit.SpatialReference_2 = ((IGeoDataset)featureClassIn).SpatialReference;

                    // Mi preparo alla copia della Feature Class
                    IFeatureDataConverter2 featureDataConverter = new FeatureDataConverterClass();
                    IEnumInvalidObject enumInvalid = 
                        featureDataConverter.ConvertFeatureClass(InDatasetName, null, null, outFeatureDatasetName, OutFCName, geometryDefOUT_Fc, fieldsOut, "", 1000, 0);

                    IInvalidObjectInfo invalidObject = enumInvalid.Next();
                    if (invalidObject != null)
                        warningErroriCopiaDati = "Feature Class copiata ma con errori!";
                }

            }
            catch (Exception)
            {

                throw;
            }
        }

        internal static double CalcolaDefaultIndexFC(IFeatureClass featureClass, out bool errore, out string messaggioErrore)
        {
            double DefaultIndexGrid = -9999;
            errore = false;
            messaggioErrore = String.Empty;
            const int fattore = 1;
            int proporzionalita = 1;

            double dblMaxDelta = 0;
            double dblMinDelta = 1000000000000d;
            double dblQuadratezzaExtent = 1;

            try
            {
                int numeroFeatures = featureClass.FeatureCount(null) - 1;
                if (numeroFeatures > 1000)
                    proporzionalita = 1000;

                if (numeroFeatures <= 0)
                {
                    DefaultIndexGrid = 1000;
                    return DefaultIndexGrid;
                }
                else
                {
                    if(featureClass.ShapeType == esriGeometryType.esriGeometryPoint || featureClass.ShapeType == esriGeometryType.esriGeometryMultipoint)
                    {
                        DefaultIndexGrid = CalcolaDefaultIndex_Fc_Point(featureClass);
                        return DefaultIndexGrid;
                    }
                    else
                    {
                        // Ricavo nome OID della Feature Class di input
                        string OID = featureClass.OIDFieldName;

                        // Ricavo Envelope della intera Feature Class
                        IGeoDataset geoDataset = featureClass as IGeoDataset;
                        IEnvelope envelope = geoDataset.Extent;

                        // Ricavo una stima sulla "quadratezza" dell'extent della Feature Class

                        dblMaxDelta = Math.Max(dblMaxDelta, Math.Max(envelope.Width, envelope.Height));
                        dblMinDelta = Math.Min(dblMinDelta, Math.Min(envelope.Width, envelope.Height));

                        if (dblMinDelta != 0)
                        {
                            dblQuadratezzaExtent =
                                1 + (Math.Min(envelope.Width, envelope.Height) / Math.Max(envelope.Width, envelope.Height));
                        }
                        else
                        {
                            dblQuadratezzaExtent = dblQuadratezzaExtent + 0.0001d;
                        }

                        if (dblQuadratezzaExtent / proporzionalita > 0.5)
                        {
                            DefaultIndexGrid = (dblMinDelta + ((dblMaxDelta - dblMinDelta) / 2)) * fattore;
                            return DefaultIndexGrid;
                        }
                        else
                        {
                            DefaultIndexGrid = Math.Round((dblMaxDelta / 2) * fattore, 3);
                            return DefaultIndexGrid;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                errore = true;
                messaggioErrore = ex.Message;
            }

            return DefaultIndexGrid;
        }

        private static double CalcolaDefaultIndex_Fc_Point(IFeatureClass featureClass)
        {
            try
            {
                // Ricavo Envelope della intera Feature Class
                IGeoDataset geoDataset = featureClass as IGeoDataset;
                IEnvelope envelope = geoDataset.Extent;

                // Calcolo la approssimata Primo Spatial Index
                int numeroFeatures = featureClass.FeatureCount(null) - 1;

                double DefaultIndexGridPoint;
                if (numeroFeatures == 0 || envelope.IsEmpty)
                {
                    DefaultIndexGridPoint = 1000;
                    return DefaultIndexGridPoint;
                }
                else
                {
                    double area = envelope.Height / envelope.Width;
                    // Approssimo l'indice spaziale come la radice quadrata dell'area fratto il numero di features
                    DefaultIndexGridPoint = Math.Sqrt(area / numeroFeatures);
                    return DefaultIndexGridPoint;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
