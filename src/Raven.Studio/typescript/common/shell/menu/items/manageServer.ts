﻿import appUrl = require("common/appUrl");
import intermediateMenuItem = require("common/shell/menu/intermediateMenuItem");
import leafMenuItem = require("common/shell/menu/leafMenuItem");
import separatorMenuItem = require("common/shell/menu/separatorMenuItem");
import accessManager = require("common/shell/accessManager");

export = getManageServerMenuItem;

function getManageServerMenuItem() {

    const access = accessManager.default.manageServerMenu;
    const accessMainMenu = accessManager.default.mainMenu;
        
    const items: menuItem[] = [
        new leafMenuItem({
            route: 'admin/settings/cluster',
            moduleId: "viewmodels/manage/cluster",
            title: "Cluster",
            nav: true,
            css: 'icon-cluster',
            dynamicHash: appUrl.forCluster,
            disableWithReason: access.disableClusterMenuItem
        }),
        new leafMenuItem({
            route: 'admin/settings/addClusterNode',
            moduleId: "viewmodels/manage/addClusterNode",
            title: "Add Cluster Node",
            nav: false,
            dynamicHash: appUrl.forAddClusterNode,
            disableWithReason: accessManager.default.disableIfNotClusterAdminOrClusterNode,
            itemRouteToHighlight: 'admin/settings/cluster'
        }),           
        new leafMenuItem({
            route: 'admin/settings/clientConfiguration',
            moduleId: 'viewmodels/manage/clientConfiguration',
            title: 'Client Configuration',
            nav: true,
            css: 'icon-client-configuration',
            dynamicHash: appUrl.forGlobalClientConfiguration,
            disableWithReason: access.disableClientConfigurationMenuItem
        }),
        new leafMenuItem({
            route: 'admin/settings/studioConfiguration',
            moduleId: 'viewmodels/manage/studioConfiguration',
            title: 'Studio Configuration',
            nav: true,
            css: 'icon-studio-configuration',
            dynamicHash: appUrl.forGlobalStudioConfiguration,
            disableWithReason: access.disableStudioConfigurationMenuItem
        }),
        new leafMenuItem({
            route: 'admin/settings/adminJsConsole',
            moduleId: "viewmodels/manage/adminJsConsole",
            title: "Admin JS Console",
            nav: true,
            css: 'icon-administrator-js-console',
            dynamicHash: appUrl.forAdminJsConsole,
            disableWithReason: access.disableAdminJSConsoleMenuItem
        }),
        new leafMenuItem({
            route: 'admin/settings/certificates',
            moduleId: "viewmodels/manage/certificates",
            title: "Certificates",
            nav: true,
            css: 'icon-certificate',
            dynamicHash: appUrl.forCertificates,
            disableWithReason: access.disableCertificatesMenuItem
        }),
        new leafMenuItem({
            route: 'admin/settings/serverWideBackupList',
            moduleId: "viewmodels/manage/serverWideBackupList",
            title: "Server-Wide Backup",
            nav: true,
            css: 'icon-server-wide-backup',
            dynamicHash: appUrl.forServerWideBackupList,
            disableWithReason: access.disableServerWideBackupMenuItem
        }),
        new leafMenuItem({
            route: 'admin/settings/editServerWideBackup',
            moduleId: "viewmodels/manage/editServerWideBackup",
            title: "Edit Server-Wide Backup Task",
            nav: false,
            dynamicHash: appUrl.forEditServerWideBackup,
            disableWithReason: accessManager.default.disableIfNotClusterAdminOrClusterNode,
            itemRouteToHighlight: 'admin/settings/serverWideBackupList'
        }),
        new separatorMenuItem(),
        new separatorMenuItem('Debug'),
        new leafMenuItem({
            route: 'admin/settings/adminLogs',
            moduleId: 'viewmodels/manage/adminLogs',
            title: 'Admin Logs',
            nav: true,
            css: 'icon-admin-logs',
            dynamicHash: appUrl.forAdminLogs,
            disableWithReason: access.disableAdminLogsMenuItem
        }),
        new leafMenuItem({
            route: 'admin/settings/trafficWatch',
            moduleId: 'viewmodels/manage/trafficWatch',
            title: 'Traffic Watch',
            nav: true,
            css: 'icon-traffic-watch',
            dynamicHash: appUrl.forTrafficWatch,
            disableWithReason: access.disableTrafficWatchMenuItem
        }),
        new leafMenuItem({
            route: 'admin/settings/debugInfo',
            moduleId: 'viewmodels/manage/infoPackage',
            title: 'Gather Debug Info',
            nav: true,
            css: 'icon-gather-debug-information',
            dynamicHash: appUrl.forDebugInfo,
            disableWithReason: access.disableGatherDebugInfoMenuItem
        }),
        new leafMenuItem({
            route: 'admin/settings/storageReport',
            moduleId: 'viewmodels/manage/storageReport',
            title: 'Storage Report',
            tooltip: "Storage Report",
            nav: true,
            css: 'icon-system-storage',
            dynamicHash: appUrl.forSystemStorageReport,
            disableWithReason: access.disableSystemStorageReport
        }),
        new leafMenuItem({
            route: 'admin/settings/ioStats',
            moduleId: 'viewmodels/manage/serverWideIoStats',
            title: 'IO Stats',
            tooltip: "Displays IO metrics status",
            nav: true,
            css: 'icon-manage-server-io-test',
            dynamicHash: appUrl.forSystemIoStats,
            disableWithReason: access.disableSystemIoStats,
        }),
        new leafMenuItem({
            route: 'admin/settings/captureStackTraces',
            moduleId: 'viewmodels/manage/captureStackTraces',
            title: 'Stack Traces',
            nav: true,
            css: 'icon-stack-traces', 
            dynamicHash: appUrl.forCaptureStackTraces,
            disableWithReason: access.disableCaptureStackTraces,
        }),
        new leafMenuItem({
            route: 'admin/settings/runningQueries',
            moduleId: 'viewmodels/manage/runningQueries',
            title: 'Running Queries',
            nav: true,
            css: 'icon-manage-server-running-queries',
            dynamicHash: appUrl.forRunningQueries
        }),
        new leafMenuItem({
            route: 'admin/settings/debug/advanced*details',
            moduleId: 'viewmodels/manage/debugAdvancedParent',
            title: 'Advanced',
            nav: true,
            css: 'icon-debug-advanced',
            hash: appUrl.forDebugAdvancedThreadsRuntime(),
            disableWithReason: access.disableAdvancedMenuItem
        }),
    ];

    return new intermediateMenuItem('Manage Server', items, 'icon-manage-server', null, accessMainMenu.showManageServerMenuItem);
}

