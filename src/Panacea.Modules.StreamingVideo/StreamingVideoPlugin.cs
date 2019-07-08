using Panacea.Core;
using Panacea.Interop;
using Panacea.Models;
using Panacea.Modularity.Billing;
using Panacea.Modularity.Content;
using Panacea.Modularity.Favorites;
using Panacea.Modularity.UiManager;
using Panacea.Modules.StreamingVideo.Models;
using Panacea.Modules.StreamingVideo.ViewModels;
using Panacea.Multilinguality;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Panacea.Modules.StreamingVideo
{
    public class StreamingVideoPlugin : ICallablePlugin, IHasFavoritesPlugin, IContentPlugin
    {
        public List<ServerItem> Favorites { get; set; }

        private PanaceaServices _core;
        private Translator _translator;

        public StreamingVideoProvider Provider {get;set;}

        public StreamingVideoPlugin(PanaceaServices core)
        {
            _core = core;
            _translator = new Translator("StreamingVideo");
            Provider = new StreamingVideoProvider(core);
        }

        public Task BeginInit()
        {
            return Task.CompletedTask;
        }

        public void Call()
        {
            if (_core.TryGetUiManager(out IUiManager ui))
            {
                var videoList = new VideoListViewModel(_core, this);
                ui.Navigate(videoList);
            }
            _core.WebSocket.PopularNotifyPage("StreamingVideo");
        }

        public void Dispose()
        {
            return;
        }

        public Task EndInit()
        {
            try
            {
                Process.Start("rundll32.exe", "InetCpl.cpl,ClearMyTracksByProcess 306");
            }
            catch
            {

            }
            _core.UserService.UserLoggedOut += UserService_UserLoggedOut;
            return Task.CompletedTask;
        }

        private Task UserService_UserLoggedOut(IUser user)
        {
            try
            {
                Process.Start("rundll32.exe", "InetCpl.cpl,ClearMyTracksByProcess 306");
            }
            catch { }
            Provider.Refresh();
            return Task.CompletedTask;
        }

        public Type GetContentType()
        {
            return typeof(VideoStreamItem);
        }

        public async Task OpenItemAsync(ServerItem item)
        {
            var streamingVideo = item as VideoStreamItem;
            if (streamingVideo == null) return;
            if (_core.TryGetBilling(out IBillingManager _billing))
            {
                if (!_billing.IsPluginFree("StreamingVideo"))
                {
                    var service = await _billing.GetOrRequestServiceForItemAsync(_translator.Translate("This Video Stream requires service."), "StreamingVideo", streamingVideo);
                    if (service == null)
                    {
                        return;
                    }
                }
            }
            _core.WebSocket.PopularNotify("StreamingVideo", "StreamingVideo", streamingVideo.Id, _core.UserService.User?.Id ?? "0");
            Open((item as VideoStreamItem).Url);
            return;
        }
        private void Open(string url)
        {
            foreach (var proc in Process.GetProcessesByName("iexplore"))
            {
                try
                {
                    proc.Kill();
                }
                catch { }
            }
            var i = new ProcessStartInfo()
            {
                FileName = "iexplore",
                Arguments = $"-k {url}",

            };
            var p = new Process()
            {
                StartInfo = i,
                EnableRaisingEvents = true
            };
            p.Exited += P_Exited;


            p.Start();
            p.BindToCurrentProcess();
            if (_core.TryGetUiManager(out IUiManager ui)){
                ui.Navigate(new IeContainerViewModel(_core, p), false);
            }
        }
        private void P_Exited(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (_core.TryGetUiManager(out IUiManager ui))
                    {
                        if (ui.CurrentPage.GetType() == typeof(IeContainerViewModel))
                        {
                            ui.GoBack();
                        }
                    }
                }
                catch { }
            });
        }
        public Task Shutdown()
        {
            return Task.CompletedTask;
        }
    }
}
