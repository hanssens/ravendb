﻿using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Operations.Backups;
using Raven.Client.Documents.Operations.ETL;
using Raven.Client.Documents.Operations.ETL.SQL;
using Raven.Client.Documents.Operations.Expiration;
using Raven.Client.Documents.Operations.Refresh;
using Raven.Client.Documents.Operations.Replication;
using Raven.Client.Documents.Operations.Revisions;
using Raven.Client.Documents.Operations.TimeSeries;
using Raven.Client.Documents.Queries.Sorting;
using Raven.Client.ServerWide;
using Sparrow.Json;

namespace Raven.Server.ServerWide
{
    public class RawDatabaseRecord : IDisposable
    {
        private BlittableJsonReaderObject _record;

        private DatabaseRecord _materializedRecord;

        public RawDatabaseRecord(BlittableJsonReaderObject record)
        {
            _record = record ?? throw new ArgumentNullException(nameof(record));
        }

        public BlittableJsonReaderObject Raw
        {
            get
            {
                if (_record == null)
                    throw new ArgumentNullException(nameof(_record));

                return _record;
            }
        }

        private bool? _isDisabled;

        public bool IsDisabled
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.Disabled;

                if (_isDisabled == null)
                {
                    _isDisabled = _record.TryGet(nameof(DatabaseRecord.Disabled), out bool disabled)
                        ? disabled
                        : false;
                }

                return _isDisabled.Value;
            }
        }

        private bool? _isEncrypted;

        public bool IsEncrypted
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.Encrypted;

                if (_isEncrypted == null)
                {
                    _isEncrypted = _record.TryGet(nameof(DatabaseRecord.Encrypted), out bool encrypted)
                        ? encrypted
                        : false;
                }

                return _isEncrypted.Value;
            }
        }

        public long? _etagForBackup;

        public long EtagForBackup
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.EtagForBackup;

                if (_etagForBackup == null)
                {
                    _etagForBackup = _record.TryGet(nameof(DatabaseRecord.EtagForBackup), out long etagForBackup)
                        ? etagForBackup
                        : 0;
                }

                return _etagForBackup.Value;
            }
        }

        public string _databaseName;

        public string DatabaseName
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.DatabaseName;

                if (_databaseName == null)
                    _record.TryGet(nameof(DatabaseRecord.DatabaseName), out _databaseName);

                return _databaseName;
            }
        }

        private DatabaseTopology _topology;

        public DatabaseTopology Topology
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.Topology;

                if (_topology == null && _record.TryGet(nameof(DatabaseRecord.Topology), out BlittableJsonReaderObject topologyJson))
                    _topology = JsonDeserializationCluster.DatabaseTopology(topologyJson);

                return _topology;
            }
        }

        private DatabaseStateStatus? _databaseState;

        public DatabaseStateStatus DatabaseState
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.DatabaseState;

                if (_databaseState == null && _record.TryGet(nameof(DatabaseRecord.DatabaseState), out _databaseState) == false)
                    _databaseState = DatabaseStateStatus.Normal;

                return _databaseState.Value;
            }
        }

        private TimeSeriesConfiguration _timeSeriesConfiguration;

        public TimeSeriesConfiguration TimeSeriesConfiguration
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.TimeSeries;
                
                if (_timeSeriesConfiguration == null && _record.TryGet(nameof(DatabaseRecord.TimeSeries), out BlittableJsonReaderObject config) && config != null)
                    _timeSeriesConfiguration = JsonDeserializationCluster.TimeSeriesConfiguration(config);

                return _timeSeriesConfiguration;
            }
        }

        private RevisionsConfiguration _revisionsConfiguration;

        public RevisionsConfiguration RevisionsConfiguration
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.Revisions;

                if (_revisionsConfiguration == null && _record.TryGet(nameof(DatabaseRecord.Revisions), out BlittableJsonReaderObject config) && config != null)
                    _revisionsConfiguration = JsonDeserializationCluster.RevisionsConfiguration(config);

                return _revisionsConfiguration;
            }
        }

        private DocumentsCompressionConfiguration _documentsCompressionConfiguration;

        public DocumentsCompressionConfiguration DocumentsCompressionConfiguration
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.DocumentsCompression;

                if (_documentsCompressionConfiguration == null && _record.TryGet(nameof(DatabaseRecord.DocumentsCompression), out BlittableJsonReaderObject config) && config != null)
                    _documentsCompressionConfiguration = JsonDeserializationCluster.DocumentsCompressionConfiguration(config);

                return _documentsCompressionConfiguration;
            }
        }
        
        private ConflictSolver _conflictSolverConfiguration;

        public ConflictSolver ConflictSolverConfiguration
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.ConflictSolverConfig;

                if (_conflictSolverConfiguration == null && _record.TryGet(nameof(DatabaseRecord.ConflictSolverConfig), out BlittableJsonReaderObject config) && config != null)
                    _conflictSolverConfiguration = JsonDeserializationCluster.ConflictSolverConfig(config);

                return _conflictSolverConfiguration;
            }
        }

        private ExpirationConfiguration _expirationConfiguration;

        public ExpirationConfiguration ExpirationConfiguration
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.Expiration;

                if (_expirationConfiguration == null && _record.TryGet(nameof(DatabaseRecord.Expiration), out BlittableJsonReaderObject config) && config != null)
                    _expirationConfiguration = JsonDeserializationCluster.ExpirationConfiguration(config);

                return _expirationConfiguration;
            }
        }

        private RefreshConfiguration _refreshConfiguration;

        public RefreshConfiguration RefreshConfiguration
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.Refresh;

                if (_refreshConfiguration == null && _record.TryGet(nameof(DatabaseRecord.Refresh), out BlittableJsonReaderObject config) && config != null)
                    _refreshConfiguration = JsonDeserializationCluster.RefreshConfiguration(config);

                return _refreshConfiguration;
            }
        }

        private List<ExternalReplication> _externalReplications;

        public List<ExternalReplication> ExternalReplications
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.ExternalReplications;

                if (_externalReplications == null)
                {
                    _externalReplications = new List<ExternalReplication>();
                    if (_record.TryGet(nameof(DatabaseRecord.ExternalReplications), out BlittableJsonReaderArray bjra) && bjra != null)
                    {
                        foreach (BlittableJsonReaderObject element in bjra)
                            _externalReplications.Add(JsonDeserializationCluster.ExternalReplication(element));
                    }
                }

                return _externalReplications;
            }
        }

        private List<PullReplicationDefinition> _hubPullReplications;

        public List<PullReplicationDefinition> HubPullReplications
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.HubPullReplications;

                if (_hubPullReplications == null)
                {
                    _hubPullReplications = new List<PullReplicationDefinition>();
                    if (_record.TryGet(nameof(DatabaseRecord.HubPullReplications), out BlittableJsonReaderArray bjra) && bjra != null)
                    {
                        foreach (BlittableJsonReaderObject element in bjra)
                            _hubPullReplications.Add(JsonDeserializationCluster.PullReplicationDefinition(element));
                    }
                }

                return _hubPullReplications;
            }
        }

        private List<long> _periodicBackupsTaskIds;

        public List<long> PeriodicBackupsTaskIds
        {
            get
            {
                if (_periodicBackupsTaskIds == null)
                {
                    if (_materializedRecord != null)
                    {
                        _periodicBackupsTaskIds = _materializedRecord
                            .PeriodicBackups
                            .Select(x => x.TaskId)
                            .ToList();
                    }
                    else
                    {
                        _periodicBackupsTaskIds = new List<long>();
                        if (_record.TryGet(nameof(DatabaseRecord.PeriodicBackups), out BlittableJsonReaderArray bjra) && bjra != null)
                        {
                            foreach (BlittableJsonReaderObject element in bjra)
                            {
                                if (element.TryGet(nameof(PeriodicBackupConfiguration.TaskId), out long taskId) == false)
                                    continue;

                                _periodicBackupsTaskIds.Add(taskId);
                            }
                        }
                    }
                }

                return _periodicBackupsTaskIds;
            }
        }

        public PeriodicBackupConfiguration GetPeriodicBackupConfiguration(long taskId)
        {
            if (_materializedRecord != null)
                return _materializedRecord.PeriodicBackups.Find(x => x.TaskId == taskId);

            if (_record.TryGet(nameof(DatabaseRecord.PeriodicBackups), out BlittableJsonReaderArray bjra) == false || bjra == null)
                return null;

            foreach (BlittableJsonReaderObject element in bjra)
            {
                if (element.TryGet(nameof(PeriodicBackupConfiguration.TaskId), out long configurationTaskId) == false)
                    continue;

                if (taskId == configurationTaskId)
                    return JsonDeserializationCluster.PeriodicBackupConfiguration(element);
            }

            return null;
        }

        private List<RavenEtlConfiguration> _ravenEtls;

        public List<RavenEtlConfiguration> RavenEtls
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.RavenEtls;

                if (_ravenEtls == null)
                {
                    _ravenEtls = new List<RavenEtlConfiguration>();
                    if (_record.TryGet(nameof(DatabaseRecord.RavenEtls), out BlittableJsonReaderArray bjra) && bjra != null)
                    {
                        foreach (BlittableJsonReaderObject element in bjra)
                            _ravenEtls.Add(JsonDeserializationCluster.RavenEtlConfiguration(element));
                    }
                }

                return _ravenEtls;
            }
        }

        private List<SqlEtlConfiguration> _sqlEtls;

        public List<SqlEtlConfiguration> SqlEtls
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.SqlEtls;

                if (_sqlEtls == null)
                {
                    _sqlEtls = new List<SqlEtlConfiguration>();
                    if (_record.TryGet(nameof(DatabaseRecord.SqlEtls), out BlittableJsonReaderArray bjra) && bjra != null)
                    {
                        foreach (BlittableJsonReaderObject element in bjra)
                            _sqlEtls.Add(JsonDeserializationCluster.SqlEtlConfiguration(element));
                    }
                }

                return _sqlEtls;
            }
        }

        private Dictionary<string, string> _settings;

        public Dictionary<string, string> Settings
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.Settings;

                if (_settings == null)
                {
                    _settings = new Dictionary<string, string>();
                    if (_record.TryGet(nameof(DatabaseRecord.Settings), out BlittableJsonReaderObject obj) && obj != null)
                    {
                        var propertyDetails = new BlittableJsonReaderObject.PropertyDetails();
                        for (var i = 0; i < obj.Count; i++)
                        {
                            obj.GetPropertyByIndex(i, ref propertyDetails);

                            if (propertyDetails.Value == null)
                                continue;

                            _settings[propertyDetails.Name] = propertyDetails.Value.ToString();
                        }
                    }
                }

                return _settings;
            }
        }

        private Dictionary<string, DeletionInProgressStatus> _deletionInProgress;

        public Dictionary<string, DeletionInProgressStatus> DeletionInProgress
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.DeletionInProgress;

                if (_deletionInProgress == null)
                {
                    _deletionInProgress = new Dictionary<string, DeletionInProgressStatus>();
                    if (_record.TryGet(nameof(DatabaseRecord.DeletionInProgress), out BlittableJsonReaderObject obj) && obj != null)
                    {
                        var propertyDetails = new BlittableJsonReaderObject.PropertyDetails();
                        for (var i = 0; i < obj.Count; i++)
                        {
                            obj.GetPropertyByIndex(i, ref propertyDetails);

                            if (propertyDetails.Value == null)
                                continue;

                            if (Enum.TryParse(propertyDetails.Value.ToString(), out DeletionInProgressStatus result))
                                _deletionInProgress[propertyDetails.Name] = result;
                        }
                    }
                }

                return _deletionInProgress;
            }
        }

        private Dictionary<string, List<IndexHistoryEntry>> _indexesHistory;

        public Dictionary<string, List<IndexHistoryEntry>> IndexesHistory
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.IndexesHistory;

                if (_indexesHistory == null)
                {
                    _indexesHistory = new Dictionary<string, List<IndexHistoryEntry>>();
                    if (_record.TryGet(nameof(DatabaseRecord.IndexesHistory), out BlittableJsonReaderObject obj) && obj != null)
                    {
                        var propertyDetails = new BlittableJsonReaderObject.PropertyDetails();
                        for (var i = 0; i < obj.Count; i++)
                        {
                            obj.GetPropertyByIndex(i, ref propertyDetails);

                            if (propertyDetails.Value == null)
                                continue;

                            if (propertyDetails.Value is BlittableJsonReaderArray bjra)
                            {
                                var list = new List<IndexHistoryEntry>();
                                foreach (BlittableJsonReaderObject element in bjra)
                                    list.Add(JsonDeserializationCluster.IndexHistoryEntry(element));

                                _indexesHistory[propertyDetails.Name] = list;
                            }
                        }
                    }
                }

                return _indexesHistory;
            }
        }

        private int? _countOfIndexes;

        public int CountOfIndexes
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.Indexes?.Count ?? 0;

                if (_countOfIndexes == null)
                {
                    _countOfIndexes = 0;
                    if (_record.TryGet(nameof(DatabaseRecord.Indexes), out BlittableJsonReaderObject obj) && obj != null)
                    {
                        var propertyDetails = new BlittableJsonReaderObject.PropertyDetails();
                        for (var i = 0; i < obj.Count; i++)
                        {
                            obj.GetPropertyByIndex(i, ref propertyDetails);

                            if (propertyDetails.Value == null)
                                continue;

                            if (propertyDetails.Value is BlittableJsonReaderObject)
                                _countOfIndexes++;
                        }
                    }
                }

                return _countOfIndexes.Value;
            }
        }

        private Dictionary<string, IndexDefinition> _indexes;

        public Dictionary<string, IndexDefinition> Indexes
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.Indexes;

                if (_indexes == null)
                {
                    _indexes = new Dictionary<string, IndexDefinition>();
                    if (_record.TryGet(nameof(DatabaseRecord.Indexes), out BlittableJsonReaderObject obj) && obj != null)
                    {
                        var propertyDetails = new BlittableJsonReaderObject.PropertyDetails();
                        for (var i = 0; i < obj.Count; i++)
                        {
                            obj.GetPropertyByIndex(i, ref propertyDetails);

                            if (propertyDetails.Value == null)
                                continue;

                            if (propertyDetails.Value is BlittableJsonReaderObject bjro)
                                _indexes[propertyDetails.Name] = JsonDeserializationCluster.IndexDefinition(bjro);
                        }
                    }
                }

                return _indexes;
            }
        }

        private Dictionary<string, AutoIndexDefinition> _autoIndexes;

        public Dictionary<string, AutoIndexDefinition> AutoIndexes
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.AutoIndexes;

                if (_autoIndexes == null)
                {
                    _autoIndexes = new Dictionary<string, AutoIndexDefinition>();
                    if (_record.TryGet(nameof(DatabaseRecord.AutoIndexes), out BlittableJsonReaderObject obj) && obj != null)
                    {
                        var propertyDetails = new BlittableJsonReaderObject.PropertyDetails();
                        for (var i = 0; i < obj.Count; i++)
                        {
                            obj.GetPropertyByIndex(i, ref propertyDetails);

                            if (propertyDetails.Value == null)
                                continue;

                            if (propertyDetails.Value is BlittableJsonReaderObject bjro)
                                _autoIndexes[propertyDetails.Name] = JsonDeserializationCluster.AutoIndexDefinition(bjro);
                        }
                    }
                }

                return _autoIndexes;
            }
        }

        private Dictionary<string, SorterDefinition> _sorters;

        public Dictionary<string, SorterDefinition> Sorters
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.Sorters;

                if (_sorters == null)
                {
                    _sorters = new Dictionary<string, SorterDefinition>();
                    if (_record.TryGet(nameof(DatabaseRecord.Sorters), out BlittableJsonReaderObject obj) && obj != null)
                    {
                        var propertyDetails = new BlittableJsonReaderObject.PropertyDetails();
                        for (var i = 0; i < obj.Count; i++)
                        {
                            obj.GetPropertyByIndex(i, ref propertyDetails);

                            if (propertyDetails.Value == null)
                                continue;

                            if (propertyDetails.Value is BlittableJsonReaderObject bjro)
                                _sorters[propertyDetails.Name] = JsonDeserializationCluster.SorterDefinition(bjro);
                        }
                    }
                }

                return _sorters;
            }
        }

        private Dictionary<string, SqlConnectionString> _sqlConnectionStrings;

        public Dictionary<string, SqlConnectionString> SqlConnectionStrings
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.SqlConnectionStrings;

                if (_sqlConnectionStrings == null)
                {
                    _sqlConnectionStrings = new Dictionary<string, SqlConnectionString>();
                    if (_record.TryGet(nameof(DatabaseRecord.SqlConnectionStrings), out BlittableJsonReaderObject obj) && obj != null)
                    {
                        var propertyDetails = new BlittableJsonReaderObject.PropertyDetails();
                        for (var i = 0; i < obj.Count; i++)
                        {
                            obj.GetPropertyByIndex(i, ref propertyDetails);

                            if (propertyDetails.Value == null)
                                continue;

                            if (propertyDetails.Value is BlittableJsonReaderObject bjro)
                                _sqlConnectionStrings[propertyDetails.Name] = JsonDeserializationCluster.SqlConnectionString(bjro);
                        }
                    }
                }

                return _sqlConnectionStrings;
            }
        }

        private Dictionary<string, RavenConnectionString> _ravenConnectionStrings;

        public Dictionary<string, RavenConnectionString> RavenConnectionStrings
        {
            get
            {
                if (_materializedRecord != null)
                    return _materializedRecord.RavenConnectionStrings;

                if (_ravenConnectionStrings == null)
                {
                    _ravenConnectionStrings = new Dictionary<string, RavenConnectionString>();
                    if (_record.TryGet(nameof(DatabaseRecord.RavenConnectionStrings), out BlittableJsonReaderObject obj) && obj != null)
                    {
                        var propertyDetails = new BlittableJsonReaderObject.PropertyDetails();
                        for (var i = 0; i < obj.Count; i++)
                        {
                            obj.GetPropertyByIndex(i, ref propertyDetails);

                            if (propertyDetails.Value == null)
                                continue;

                            if (propertyDetails.Value is BlittableJsonReaderObject bjro)
                                _ravenConnectionStrings[propertyDetails.Name] = JsonDeserializationCluster.RavenConnectionString(bjro);
                        }
                    }
                }

                return _ravenConnectionStrings;
            }
        }

        public void Dispose()
        {
            _record?.Dispose();
            _record = null;
        }

        public DatabaseRecord MaterializedRecord
        {
            get
            {
                if (_materializedRecord == null)
                {
                    _materializedRecord = JsonDeserializationCluster.DatabaseRecord(_record);
                    Dispose();
                }

                return _materializedRecord;
            }
        }
    }
}
