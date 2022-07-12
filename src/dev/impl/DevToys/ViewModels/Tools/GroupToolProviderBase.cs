﻿#nullable enable

using System.Linq;
using DevToys.Api.Tools;
using DevToys.Core.Threading;
using DevToys.Shared.Api.Core;
using Windows.UI.Xaml.Controls;

namespace DevToys.ViewModels.Tools
{
    internal abstract class GroupToolProviderBase : ToolProviderBase, IToolProvider
    {
        private readonly IMefProvider _mefProvider;

        public virtual string? SearchDisplayName => MenuDisplayName;

        public virtual string? Description { get; } = null;

        public abstract string MenuDisplayName { get; }

        public abstract string AccessibleName { get; }

        public virtual string? SearchKeywords { get; }

        public abstract string IconGlyph { get; }

        public GroupToolProviderBase(IMefProvider mefProvider)
        {
            _mefProvider = mefProvider;
        }

        public bool CanBeTreatedByTool(string data)
        {
            return false;
        }

        public virtual IToolViewModel CreateTool()
        {
            return
                 new GroupToolViewModel(
                     _mefProvider
                         .Import<IToolProviderFactory>()
                         .GetAllChildrenTools(this));
        }
    }
}
