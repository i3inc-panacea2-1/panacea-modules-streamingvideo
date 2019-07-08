using Panacea.Controls;
using Panacea.Core;
using Panacea.Modularity.Favorites;
using Panacea.Modules.StreamingVideo.Models;
using Panacea.Modules.StreamingVideo.Views;
using Panacea.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Panacea.Modules.StreamingVideo.ViewModels
{
    [View(typeof(VideoList))]
    public class VideoListViewModel : ViewModelBase
    {
        private PanaceaServices _core;
        private StreamingVideoPlugin _plugin;
        public StreamingVideoProvider Provider { get; set; }
        public ICommand OpenItemCommand { get; private set; }
        public ICommand IsFavoriteCommand { get; private set; }
        public AsyncCommand FavoriteCommand { get; private set; }
        public VideoListViewModel(PanaceaServices core, StreamingVideoPlugin plugin)
        {
            _core = core;
            _plugin = plugin;
            Provider = plugin.Provider;
            SetupCommands();
        }

        private void SetupCommands()
        {
            OpenItemCommand = new RelayCommand(async (arg) =>
            {
                if (_plugin == null) return;
                await _plugin.OpenItemAsync(arg as VideoStreamItem);
            });
            IsFavoriteCommand = new RelayCommand((arg) =>
            {
            }, (arg) =>
            {
                var game = arg as VideoStreamItem;
                if (_plugin.Favorites == null) return false;
                return _plugin.Favorites.Any(l => l.Id == game.Id);
            });

            FavoriteCommand = new AsyncCommand(async (args) =>
            {
                var game = args as VideoStreamItem;
                if (game == null) return;
                if (_core.TryGetFavoritesPlugin(out IFavoritesManager _favoritesManager))
                {
                    try
                    {
                        if (await _favoritesManager.AddOrRemoveFavoriteAsync("StreamingVideo", game))
                            OnPropertyChanged(nameof(IsFavoriteCommand));
                    }
                    catch (Exception e)
                    {
                        _core.Logger.Error(this, e.Message);
                    }
                }
            });
        }
    }
}
