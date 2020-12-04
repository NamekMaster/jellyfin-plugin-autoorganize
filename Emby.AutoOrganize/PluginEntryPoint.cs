using System;
using System.Threading.Tasks;
using Emby.AutoOrganize.Core;
using Emby.AutoOrganize.Data;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.AutoOrganize
{
    /// <summary>
    /// Entry point for the <see cref="AutoOrganizePlugin"/>.
    /// </summary>
    public sealed class PluginEntryPoint : IServerEntryPoint
    {
        private readonly ISessionManager _sessionManager;
        private readonly ITaskManager _taskManager;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<PluginEntryPoint> _logger;
        private readonly ILibraryMonitor _libraryMonitor;
        private readonly ILibraryManager _libraryManager;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly IProviderManager _providerManager;
        private readonly IJsonSerializer _json;

        private IFileOrganizationRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginEntryPoint"/> class.
        /// </summary>
        /// <param name="sessionManager">Instance of the <see cref="ISessionManager"/> interface.</param>
        /// <param name="taskManager">Instance of the <see cref="ITaskManager"/> interface.</param>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        /// <param name="libraryMonitor">Instance of the <see cref="ILibraryMonitor"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="config">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="json">Instance of the <see cref="IJsonSerializer"/> interface.</param>
        public PluginEntryPoint(
            ISessionManager sessionManager,
            ITaskManager taskManager,
            ILoggerFactory loggerFactory,
            ILibraryMonitor libraryMonitor,
            ILibraryManager libraryManager,
            IServerConfigurationManager config,
            IFileSystem fileSystem,
            IProviderManager providerManager,
            IJsonSerializer json)
        {
            _sessionManager = sessionManager;
            _taskManager = taskManager;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<PluginEntryPoint>();
            _libraryMonitor = libraryMonitor;
            _libraryManager = libraryManager;
            _config = config;
            _fileSystem = fileSystem;
            _providerManager = providerManager;
            _json = json;
        }

        /// <summary>
        /// Gets a reference to the current instance of the plugin instantiated by the server.
        /// </summary>
        public static PluginEntryPoint Current { get; private set; }

        /// <summary>
        /// Gets the file organization service.
        /// </summary>
        public IFileOrganizationService FileOrganizationService { get; private set; }

        /// <inheritdoc/>
        public Task RunAsync()
        {
            try
            {
                _repository = GetRepository();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing auto-organize database");
            }

            Current = this;
            FileOrganizationService = new FileOrganizationService(_taskManager, _repository, _loggerFactory, _libraryMonitor, _libraryManager, _config, _fileSystem, _providerManager);

            // Convert Config
            _config.ConvertSmartMatchInfo(FileOrganizationService);

            return Task.CompletedTask;
        }

        private IFileOrganizationRepository GetRepository()
        {
            var repo = new SqliteFileOrganizationRepository(
                _loggerFactory.CreateLogger<SqliteFileOrganizationRepository>(),
                _config.ApplicationPaths,
                _json);

            repo.Initialize();

            return repo;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _taskManager?.Dispose();
            _loggerFactory?.Dispose();
            _libraryMonitor?.Dispose();
        }
    }
}
