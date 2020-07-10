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

        /// <summary>
        /// Data una workspace di input ricava una Workspace Name
        /// </summary>
        /// <param name="workspace"></param>
        /// <returns></returns>
        internal static IWorkspaceName RicavaWorkspaceName(IWorkspace workspace)
        {
            IDataset dataset = (IDataset)workspace;
            IName name = dataset.FullName;
            IWorkspaceName workspaceName = (IWorkspaceName)name;
            return workspaceName;
        }

        internal static IName RicavaDatasetName(IDataset datasetInput, IWorkspaceName workspaceName)
        {
            IDatasetName datasetName = null;
            switch (datasetInput.Type)
            {
                case esriDatasetType.esriDTFeatureDataset:
                    IFeatureDatasetName InFeatureDatasetName = new FeatureDatasetNameClass();
                    datasetName = (IDatasetName)InFeatureDatasetName;
                    break;

                case esriDatasetType.esriDTFeatureClass:
                    IFeatureClassName InFeatureClassName = new FeatureClassNameClass();
                    datasetName = (IDatasetName)InFeatureClassName;
                    break;
                case esriDatasetType.esriDTTable:
                    ITableName InTableName = new TableNameClass();
                    datasetName = (IDatasetName)InTableName;
                    break;
                case esriDatasetType.esriDTGeometricNetwork:
                    IGeometricNetworkName InGeometricNetworkName = new GeometricNetworkNameClass();
                    datasetName = (IDatasetName)InGeometricNetworkName;
                    break;
                default:
                    return null;
            }

            // Set the name and workspace name of the new name object.
            datasetName.Name = datasetInput.Name;
            datasetName.WorkspaceName = workspaceName;
            // Cast the object to the IName interface and return it.
            IName name = (IName)datasetName;
            return name;
        }

        internal static IEnumName RicavaDatasetNameEnum(List<IName> nameList)
        {
            // Create the enumerator and cast it to the IEnumNameEdit interface.
            IEnumName enumName = new NamesEnumerator();
            IEnumNameEdit enumNameEdit = (IEnumNameEdit)enumName;
            // Add the input name objects to the enumerator and return it.
            foreach (IName name in nameList)
            {
                enumNameEdit.Add(name);
            }
            return enumName;
        }

        internal static void CopiaFeatureClass(IEnumName enumSourceName, IName targetName)
        {
            // Create the transfer object and a reference to a mapping enumerator.
            IGeoDBDataTransfer geoDBDataTransfer = new GeoDBDataTransfer();
            IEnumNameMapping[] enumNameMapping = new IEnumNameMapping[1];
            // See if the transfer can proceed with the datasets' existing names.
            _ = geoDBDataTransfer.GenerateNameMapping(enumSourceName, targetName, out enumNameMapping[0]);

            geoDBDataTransfer.Transfer(enumNameMapping[0], targetName);

        }

    }
}
