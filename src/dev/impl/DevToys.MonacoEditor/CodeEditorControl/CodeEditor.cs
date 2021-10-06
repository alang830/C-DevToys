﻿#nullable enable

using Collections.Generic;
using DevToys.MonacoEditor.Extensions;
using DevToys.MonacoEditor.Helpers;
using DevToys.MonacoEditor.Monaco;
using DevToys.MonacoEditor.Monaco.Editor;
using DevToys.MonacoEditor.Monaco.Helpers;
using Newtonsoft.Json;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace DevToys.MonacoEditor.CodeEditorControl
{
    /// <summary>
    /// UWP Windows Runtime Component wrapper for the Monaco CodeEditor
    /// https://microsoft.github.io/monaco-editor/
    /// This file contains Monaco IEditor method implementations we can call on our control.
    /// https://microsoft.github.io/monaco-editor/api/interfaces/monaco.editor.ieditor.html
    /// https://microsoft.github.io/monaco-editor/api/interfaces/monaco.editor.icommoncodeeditor.html
    /// </summary>
    [TemplatePart(Name = "View", Type = typeof(WebView))]
    public sealed partial class CodeEditor : Control, INotifyPropertyChanged, IDisposable
    {
        private readonly DisposableSempahore _mutexMarkers = new DisposableSempahore();
        private readonly DisposableSempahore _mutexLineDecorations = new DisposableSempahore();

        internal ParentAccessor? _parentAccessor;
        private bool _initialized;
        private WebView _view;
        private ModelHelper? _model;
        private ThemeListener _themeListener;
        private KeyboardListener _keyboardListener;
        private long _themeToken;

        public bool IsSettingValue { get; set; }

        public static DependencyProperty TextProperty { get; }
            = DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(CodeEditor),
                new PropertyMetadata(
                    string.Empty,
                    (d, e) =>
                    {
                        if (!((CodeEditor)d).IsSettingValue)
                        {
                            _ = ((CodeEditor)d).InvokeScriptAsync("updateContent", e.NewValue.ToString());
                        }
                    }));

        /// <summary>
        /// Get or Set the CodeEditor Text.
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static DependencyProperty SelectedTextProperty { get; } 
            = DependencyProperty.Register(
                nameof(SelectedText),
                typeof(string),
                typeof(CodeEditor),
                new PropertyMetadata(
                    string.Empty,
                    (d, e) =>
                    {
                        if (!((CodeEditor)d).IsSettingValue)
                        {
                            _ = ((CodeEditor)d).InvokeScriptAsync("updateSelectedContent", e.NewValue.ToString());
                        }
                    }));

        /// <summary>
        /// Get the current Primary Selected CodeEditor Text.
        /// </summary>
        public string SelectedText
        {
            get => (string)GetValue(SelectedTextProperty);
            set => SetValue(SelectedTextProperty, value);
        }

        public static DependencyProperty SelectedRangeProperty { get; } 
            = DependencyProperty.Register(
                nameof(SelectedRange),
                typeof(Selection),
                typeof(CodeEditor),
                new PropertyMetadata(null));

        public Selection SelectedRange
        {
            get => (Selection)GetValue(SelectedRangeProperty);
            set => SetValue(SelectedRangeProperty, value);
        }

        internal static DependencyProperty CodeLanguageProperty { get; }
            = DependencyProperty.Register(
                nameof(CodeLanguage),
                typeof(string),
                typeof(CodeEditor),
                new PropertyMetadata(
                    "xml",
                    (d, e) =>
                    {
                        if (!(d is CodeEditor editor))
                        {
                            return;
                        }

                        if (editor.Options != null)
                        {
                            editor.Options.Language = e.NewValue.ToString();
                        } 
                    }));

        /// <summary>
        /// Set the Syntax Language for the Code CodeEditor.
        /// 
        /// Note: Most likely to change or move location.
        /// </summary>
        public string CodeLanguage
        {
            get => (string)GetValue(CodeLanguageProperty);
            set => SetValue(CodeLanguageProperty, value);
        }

        internal static DependencyProperty ReadOnlyProperty { get; }
            = DependencyProperty.Register(
                nameof(ReadOnly),
                typeof(bool),
                typeof(CodeEditor),
                new PropertyMetadata(
                    false,
                    (d, e) =>
                    {
                        if (!(d is CodeEditor editor))
                        {
                            return;
                        }

                        if (editor.Options != null)
                        {
                            editor.Options.ReadOnly = bool.Parse(e.NewValue?.ToString() ?? "false");
                        } 
                    }));

        /// <summary>
        /// Set the ReadOnly option for the Code CodeEditor.
        /// </summary>
        public bool ReadOnly
        {
            get => (bool)GetValue(ReadOnlyProperty);
            set => SetValue(ReadOnlyProperty, value);
        }

        public static DependencyProperty OptionsProperty { get; } 
            = DependencyProperty.Register(
                nameof(Options),
                typeof(StandaloneEditorConstructionOptions),
                typeof(CodeEditor),
                new PropertyMetadata(
                    new StandaloneEditorConstructionOptions(),
                    (d, e) =>
                    {
                        if (d is CodeEditor editor)
                        {
                            if (e.OldValue is StandaloneEditorConstructionOptions oldValue)
                            {
                                oldValue.PropertyChanged -= editor.Options_PropertyChanged;
                            }

                            if (e.NewValue is StandaloneEditorConstructionOptions value)
                            {
                                value.PropertyChanged += editor.Options_PropertyChanged;
                            }
                        }
                    }));

        /// <summary>
        /// Get or set the CodeEditor Options. Node: Will overwrite CodeLanguage.
        /// </summary>
        public StandaloneEditorConstructionOptions Options
        {
            get => (StandaloneEditorConstructionOptions)GetValue(OptionsProperty);
            set => SetValue(OptionsProperty, value);
        }

        public static DependencyProperty HasGlyphMarginProperty { get; }
            = DependencyProperty.Register(
                nameof(HasGlyphMargin),
                typeof(bool),
                typeof(CodeEditor),
                new PropertyMetadata(
                    false,
                    (d, e) =>
                    {
                        ((CodeEditor)d).Options.GlyphMargin = e.NewValue as bool?;
                    }));

        /// <summary>
        /// Get or Set the CodeEditor Text.
        /// </summary>
        public bool HasGlyphMargin
        {
            get => (bool)GetValue(HasGlyphMarginProperty);
            set => SetValue(HasGlyphMarginProperty, value);
        }

        public static DependencyProperty DecorationsProperty { get; }
            = DependencyProperty.Register(
                nameof(Decorations),
                typeof(ModelDeltaDecoration),
                typeof(CodeEditor),
                new PropertyMetadata(
                    null,
                    async (d, e) =>
                    {
                        if (d is CodeEditor editor)
                        {
                            // We only want to do this one at a time per editor.
                            using (await editor._mutexLineDecorations.WaitAsync(CancellationToken.None))
                            {
                                var old = e.OldValue as IObservableVector<ModelDeltaDecoration>;
                                // Clear out the old line decorations if we're replacing them or setting back to null
                                if ((old != null && old.Count > 0) ||
                                         e.NewValue == null)
                                {
                                    await editor.DeltaDecorationsHelperAsync(null);
                                }

                                if (e.NewValue is IObservableVector<ModelDeltaDecoration> value)
                                {
                                    if (value.Count > 0)
                                    {
                                        await editor.DeltaDecorationsHelperAsync(value.ToArray());
                                    }

                                    value.VectorChanged -= editor.Decorations_VectorChanged;
                                    value.VectorChanged += editor.Decorations_VectorChanged;
                                }
                            }
                        }
                    }));

        private async void Decorations_VectorChanged(IObservableVector<ModelDeltaDecoration> sender, IVectorChangedEventArgs @event)
        {
            if (sender != null)
            {
                // Need to recall mutex as this is called from outside of this initial callback setting it up.
                using (await _mutexLineDecorations.WaitAsync(CancellationToken.None))
                {
                    await DeltaDecorationsHelperAsync(sender.ToArray());
                }
            }
        }

        /// <summary>
        /// Gets or sets text Decorations.
        /// </summary>
        public IObservableVector<ModelDeltaDecoration> Decorations
        {
            get => (IObservableVector<ModelDeltaDecoration>)GetValue(DecorationsProperty);
            set => SetValue(DecorationsProperty, value);
        }

        public static DependencyProperty MarkersProperty { get; } 
            = DependencyProperty.Register(
                nameof(Markers),
                typeof(IMarkerData),
                typeof(CodeEditor),
                new PropertyMetadata(
                    null,
                    async (d, e) =>
                    {
                        if (d is CodeEditor editor)
                        {
                            // We only want to do this one at a time per editor.
                            using (await editor._mutexMarkers.WaitAsync(CancellationToken.None))
                            {
                                var old = e.OldValue as IObservableVector<IMarkerData>;
                                // Clear out the old markers if we're replacing them or setting back to null
                                if ((old != null && old.Count > 0) ||
                                         e.NewValue == null)
                                {
                                    // TODO: Can I simplify this in this case?
                                    await editor.SetModelMarkersAsync("CodeEditor", Array.Empty<IMarkerData>());
                                }

                                if (e.NewValue is IObservableVector<IMarkerData> value)
                                {
                                    if (value.Count > 0)
                                    {
                                        await editor.SetModelMarkersAsync("CodeEditor", value.ToArray());
                                    }

                                    value.VectorChanged -= editor.Markers_VectorChanged;
                                    value.VectorChanged += editor.Markers_VectorChanged;
                                }
                            }
                        }
                    }));

        private async void Markers_VectorChanged(IObservableVector<IMarkerData> sender, IVectorChangedEventArgs @event)
        {
            if (sender != null)
            {
                // Need to recall mutex as this is called from outside of this initial callback setting it up.
                using (await _mutexMarkers.WaitAsync(CancellationToken.None))
                {
                    await SetModelMarkersAsync("CodeEditor", sender.ToArray());
                }
            }
        }

        /// <summary>
        /// Gets or sets the hint Markers.
        /// Note: This property is a helper for <see cref="SetModelMarkersAsync(string, IMarkerData[])"/>; use this property or the method, not both.
        /// </summary>
        public IObservableVector<IMarkerData> Markers
        {
            get => (IObservableVector<IMarkerData>)GetValue(MarkersProperty);
            set => SetValue(MarkersProperty, value);
        }

        /// <summary>
        /// When Editor is Loading, it is ready to receive commands to the Monaco Engine.
        /// </summary>
        public new event RoutedEventHandler Loading;

        /// <summary>
        /// When Editor is Loaded, it has been rendered and is ready to be displayed.
        /// </summary>
        public new event RoutedEventHandler Loaded;

        /// <summary>
        /// Called when a link is Ctrl+Clicked on in the editor, set Handled to true to prevent opening.
        /// </summary>
        public event TypedEventHandler<WebView, WebViewNewWindowRequestedEventArgs> OpenLinkRequested;

        /// <summary>
        /// Called when an internal exception is encountered while executing a command. (for testing/reporting issues)
        /// </summary>
        public event TypedEventHandler<CodeEditor, Exception> InternalException;

        /// <summary>
        /// Custom Keyboard Handler.
        /// </summary>
        public new event WebKeyEventHandler KeyDown;

        public event PropertyChangedEventHandler PropertyChanged;

        public static DependencyProperty IsEditorLoadedProperty { get; }
            = DependencyProperty.Register(
                nameof(IsEditorLoaded),
                typeof(string),
                typeof(CodeEditor),
                new PropertyMetadata(false));

        /// <summary>
        /// Template Property used during loading to prevent blank control visibility when it's still loading WebView.
        /// </summary>
        public bool IsEditorLoaded
        {
            get => (bool)GetValue(IsEditorLoadedProperty);
            private set => SetValue(IsEditorLoadedProperty, value);
        }

        /// <summary>
        /// Construct a new IStandAloneCodeEditor.
        /// </summary>
        public CodeEditor()
        {
            DefaultStyleKey = typeof(CodeEditor);
            if (Options != null)
            {
                // Set Pass-Thru Properties
                Options.GlyphMargin = HasGlyphMargin;

                // Register for changes
                Options.PropertyChanged += Options_PropertyChanged;
            }

            // Initialize this here so property changed event will fire and register collection changed event.
            Decorations = new ObservableVector<ModelDeltaDecoration>();
            Markers = new ObservableVector<IMarkerData>();
            _model = new ModelHelper(this);

            base.Loaded += CodeEditor_Loaded;
            Unloaded += CodeEditor_Unloaded;
        }

        private async void Options_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!(sender is StandaloneEditorConstructionOptions options))
            {
                return;
            }

            if (e.PropertyName == nameof(StandaloneEditorConstructionOptions.Language))
            {
                await InvokeScriptAsync("updateLanguage", options.Language);
                if (CodeLanguage != options.Language)
                {
                    CodeLanguage = options.Language;
                }
            }

            if (e.PropertyName == nameof(StandaloneEditorConstructionOptions.GlyphMargin))
            {
                if (HasGlyphMargin != options.GlyphMargin)
                {
                    options.GlyphMargin = HasGlyphMargin;
                }
            }

            if (e.PropertyName == nameof(StandaloneEditorConstructionOptions.ReadOnly))
            {
                if (ReadOnly != options.ReadOnly)
                {
                    options.ReadOnly = ReadOnly;
                }
            }

            await InvokeScriptAsync("updateOptions", options);
        }

        private void CodeEditor_Loaded(object sender, RoutedEventArgs e)
        {
            // Do this the 2nd time around.
            if (_model == null && _view != null)
            {
                _model = new ModelHelper(this);

                Options.PropertyChanged += Options_PropertyChanged;

                Decorations.VectorChanged += Decorations_VectorChanged;
                Markers.VectorChanged += Markers_VectorChanged;

                _view.NewWindowRequested += WebView_NewWindowRequested;

                _initialized = true;

                Loading?.Invoke(this, new RoutedEventArgs());

                Unloaded += CodeEditor_Unloaded;

                Loaded?.Invoke(this, new RoutedEventArgs());
            }
        }

        private void CodeEditor_Unloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= CodeEditor_Unloaded;

            if (_view != null)
            {
                _view.NavigationStarting -= WebView_NavigationStarting;
                _view.DOMContentLoaded -= WebView_DOMContentLoaded;
                _view.NavigationCompleted -= WebView_NavigationCompleted;
                _view.NewWindowRequested -= WebView_NewWindowRequested;
                _initialized = false;
            }

            Decorations.VectorChanged -= Decorations_VectorChanged;
            Markers.VectorChanged -= Markers_VectorChanged;

            Options.PropertyChanged -= Options_PropertyChanged;

            if (_themeListener != null)
            {
                _themeListener.ThemeChanged -= ThemeListener_ThemeChanged;
            }

            _themeListener = null;

            UnregisterPropertyChangedCallback(RequestedThemeProperty, _themeToken);
            _keyboardListener = null;
            _model = null;
        }

        protected override void OnApplyTemplate()
        {
            if (_view != null)
            {
                _view.NavigationStarting -= WebView_NavigationStarting;
                _view.DOMContentLoaded -= WebView_DOMContentLoaded;
                _view.NavigationCompleted -= WebView_NavigationCompleted;
                _view.NewWindowRequested -= WebView_NewWindowRequested;
                _initialized = false;
            }

            _view = (WebView)GetTemplateChild("View");

            if (_view != null)
            {
                _view.NavigationStarting += WebView_NavigationStarting;
                _view.DOMContentLoaded += WebView_DOMContentLoaded;
                _view.NavigationCompleted += WebView_NavigationCompleted;
                _view.NewWindowRequested += WebView_NewWindowRequested;
                _view.Source = new System.Uri("ms-appx-web:///DevToys.MonacoEditor/CodeEditor/CodeEditor.html");
            }

            base.OnApplyTemplate();
        }

        internal async Task SendScriptAsync(string script,
            [CallerMemberName] string? member = null,
            [CallerFilePath] string? file = null,
            [CallerLineNumber] int line = 0)
        {
            await SendScriptAsync<object>(script, member, file, line);
        }

        internal async Task<T?> SendScriptAsync<T>(string script,
            [CallerMemberName] string? member = null,
            [CallerFilePath] string? file = null,
            [CallerLineNumber] int line = 0)
        {
            if (_initialized)
            {
                try
                {
                    return await _view.RunScriptAsync<T>(script, member, file, line);
                }
                catch (Exception e)
                {
                    InternalException?.Invoke(this, e);
                }
            }
            else
            {
#if DEBUG
                Debug.WriteLine("WARNING: Tried to call '" + script + "' before initialized.");
#endif
            }

            return default;
        }

        internal async Task InvokeScriptAsync(
            string method,
            object arg,
            bool serialize = true,
            [CallerMemberName] string? member = null,
            [CallerFilePath] string? file = null,
            [CallerLineNumber] int line = 0)
        {
            await InvokeScriptAsync<object>(method, new object[] { arg }, serialize, member, file, line);
        }

        internal async Task InvokeScriptAsync(
            string method,
            object[] args,
            bool serialize = true,
            [CallerMemberName] string? member = null,
            [CallerFilePath] string? file = null,
            [CallerLineNumber] int line = 0)
        {
            await InvokeScriptAsync<object>(method, args, serialize, member, file, line);
        }

        internal async Task<T?> InvokeScriptAsync<T>(
            string method,
            object arg,
            bool serialize = true,
            [CallerMemberName] string? member = null,
            [CallerFilePath] string? file = null,
            [CallerLineNumber] int line = 0)
        {
            return await InvokeScriptAsync<T>(method, new object[] { arg }, serialize, member, file, line);
        }

        internal async Task<T?> InvokeScriptAsync<T>(
            string method,
            object[] args,
            bool serialize = true,
            [CallerMemberName] string? member = null,
            [CallerFilePath] string? file = null,
            [CallerLineNumber] int line = 0)
        {
            if (_initialized)
            {
                try
                {
                    return await _view.InvokeScriptAsync<T>(method, args, serialize, member, file, line);
                }
                catch (Exception e)
                {
                    InternalException?.Invoke(this, e);
                }
            }
            else
            {
#if DEBUG
                Debug.WriteLine("WARNING: Tried to call " + method + " before initialized.");
#endif
            }

            return default;
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _parentAccessor?.Dispose();
            _parentAccessor = null;
            CssStyleBroker.DetachEditor(this);
        }

        private void WebView_DOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args)
        {
#if DEBUG
            Debug.WriteLine("DOM Content Loaded");
#endif
            _initialized = true;
        }

        private async void WebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            IsEditorLoaded = true;

            // Make sure inner editor is focused
            await SendScriptAsync("editor.focus();");

            // If we're supposed to have focus, make sure we try and refocus on our now loaded webview.
            if (FocusManager.GetFocusedElement() == this)
            {
                _view.Focus(FocusState.Programmatic);
            }

            Loaded?.Invoke(this, new RoutedEventArgs());
        }
        private void WebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
#if DEBUG
            Debug.WriteLine("Navigation Starting");
#endif
            _parentAccessor = new ParentAccessor(this);
            _parentAccessor.AddAssemblyForTypeLookup(typeof(Range).GetTypeInfo().Assembly);
            _parentAccessor.RegisterAction("Loaded", CodeEditorLoaded);

            _themeListener = new ThemeListener();
            _themeListener.ThemeChanged += ThemeListener_ThemeChanged;
            _themeToken = RegisterPropertyChangedCallback(RequestedThemeProperty, RequestedTheme_PropertyChanged);

            _keyboardListener = new KeyboardListener(this);

            _view.AddWebAllowedObject("Debug", new DebugLogger());
            _view.AddWebAllowedObject("Parent", _parentAccessor);
            _view.AddWebAllowedObject("Theme", _themeListener);
            _view.AddWebAllowedObject("Keyboard", _keyboardListener);
        }

        private async void CodeEditorLoaded()
        {
            if (Decorations != null && Decorations.Count > 0)
            {
                // Need to retrigger highlights after load if they were set before load.
                await DeltaDecorationsHelperAsync(Decorations.ToArray());
            }

            // Now we're done loading
            Loading?.Invoke(this, new RoutedEventArgs());
        }

        private void WebView_NewWindowRequested(WebView sender, WebViewNewWindowRequestedEventArgs args)
        {
            // TODO: Should probably create own event args here as we don't want to expose the referrer to our internal page?
            OpenLinkRequested?.Invoke(sender, args);
        }

        private async void RequestedTheme_PropertyChanged(DependencyObject obj, DependencyProperty property)
        {
            var editor = (CodeEditor)obj;
            var theme = editor.RequestedTheme;
            var tstr = string.Empty;

            if (theme == ElementTheme.Default)
            {
                tstr = _themeListener.CurrentThemeName;
            }
            else
            {
                tstr = theme.ToString();
            }

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                await InvokeScriptAsync("changeTheme", new string[] { tstr, _themeListener.IsHighContrast.ToString() });
            });
        }

        private async void ThemeListener_ThemeChanged(ThemeListener sender)
        {
            if (RequestedTheme == ElementTheme.Default)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    await InvokeScriptAsync("changeTheme", args: new string[] { sender.CurrentTheme.ToString(), sender.IsHighContrast.ToString() });
                });
            }
        }

        internal bool TriggerKeyDown(WebKeyEventArgs args)
        {
            KeyDown?.Invoke(this, args);

            return args.Handled;
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);

            if (_view != null && FocusManager.GetFocusedElement() == this)
            {
                // Forward Focus onto our inner WebView
                _view.Focus(FocusState.Programmatic);
            }
        }

        #region Reveal Methods

        public IAsyncAction RevealLineAsync(uint lineNumber)
        {
            return SendScriptAsync("editor.revealLine(" + lineNumber + ")").AsAsyncAction();
        }

        public IAsyncAction RevealLineInCenterAsync(uint lineNumber)
        {
            return SendScriptAsync("editor.revealLineInCenter(" + lineNumber + ")").AsAsyncAction();
        }

        public IAsyncAction RevealLineInCenterIfOutsideViewportAsync(uint lineNumber)
        {
            return SendScriptAsync("editor.revealLineInCenterIfOutsideViewport(" + lineNumber + ")").AsAsyncAction();
        }

        public IAsyncAction RevealLinesAsync(uint startLineNumber, uint endLineNumber)
        {
            return SendScriptAsync("editor.revealLines(" + startLineNumber + ", " + endLineNumber + ")").AsAsyncAction();
        }

        public IAsyncAction RevealLinesInCenterAsync(uint startLineNumber, uint endLineNumber)
        {
            return SendScriptAsync("editor.revealLinesInCenter(" + startLineNumber + ", " + endLineNumber + ")").AsAsyncAction();
        }

        public IAsyncAction RevealLinesInCenterIfOutsideViewportAsync(uint startLineNumber, uint endLineNumber)
        {
            return SendScriptAsync("editor.revealLinesInCenterIfOutsideViewport(" + startLineNumber + ", " + endLineNumber + ")").AsAsyncAction();
        }

        public IAsyncAction RevealPositionAsync(IPosition position)
        {
            return RevealPositionAsync(position, false, false);
        }

        public IAsyncAction RevealPositionAsync(IPosition position, bool revealVerticalInCenter)
        {
            return RevealPositionAsync(position, revealVerticalInCenter, false);
        }

        public IAsyncAction RevealPositionAsync(IPosition position, bool revealVerticalInCenter, bool revealHorizontal)
        {
            return SendScriptAsync("editor.revealPosition(JSON.parse('" + position.ToJson() + "'), " + JsonConvert.ToString(revealVerticalInCenter) + ", " + JsonConvert.ToString(revealHorizontal) + ")").AsAsyncAction();
        }

        public IAsyncAction RevealPositionInCenterAsync(IPosition position)
        {
            return SendScriptAsync("editor.revealPositionInCenter(JSON.parse('" + position.ToJson() + "'))").AsAsyncAction();
        }

        public IAsyncAction RevealPositionInCenterIfOutsideViewportAsync(IPosition position)
        {
            return SendScriptAsync("editor.revealPositionInCenterIfOutsideViewport(JSON.parse('" + position.ToJson() + "'))").AsAsyncAction();
        }

        public IAsyncAction RevealRangeAsync(IRange range)
        {
            return SendScriptAsync("editor.revealRange(JSON.parse('" + range.ToJson() + "'))").AsAsyncAction();
        }

        public IAsyncAction RevealRangeAtTopAsync(IRange range)
        {
            return SendScriptAsync("editor.revealRangeAtTop(JSON.parse('" + range.ToJson() + "'))").AsAsyncAction();
        }

        public IAsyncAction RevealRangeInCenterAsync(IRange range)
        {
            return SendScriptAsync("editor.revealRangeInCenter(JSON.parse('" + range.ToJson() + "'))").AsAsyncAction();
        }

        public IAsyncAction RevealRangeInCenterIfOutsideViewportAsync(IRange range)
        {
            return SendScriptAsync("editor.revealRangeInCenterIfOutsideViewport(JSON.parse('" + range.ToJson() + "'))").AsAsyncAction();
        }
        #endregion

        public IAsyncAction AddActionAsync(IActionDescriptor action)
        {
            var wref = new WeakReference<CodeEditor>(this);
            _parentAccessor.RegisterAction("Action" + action.Id, new Action(() => { if (wref.TryGetTarget(out CodeEditor editor)) { action?.Run(editor, null); } }));
            return InvokeScriptAsync("addAction", action).AsAsyncAction();
        }

        /// <summary>
        /// Invoke scripts, return value must be strings
        /// </summary>
        /// <param name="script">Script to invoke</param>
        /// <returns>An async operation result to string</returns>
        public IAsyncOperation<string> InvokeScriptAsync(string script)
        {
            return _view.InvokeScriptAsync("eval", new[] { script });
        }

        public IAsyncOperation<string> AddCommandAsync(int keybinding, CommandHandler handler)
        {
            return AddCommandAsync(keybinding, handler, string.Empty);
        }

        public IAsyncOperation<string> AddCommandAsync(int keybinding, CommandHandler handler, string context)
        {
            var name = "Command" + keybinding;
            _parentAccessor?.RegisterAction(name, new Action(() => { handler?.Invoke(); }));
            return InvokeScriptAsync<string>("addCommand", new object[] { keybinding, name, context }).AsAsyncOperation();
        }

        public IAsyncOperation<ContextKey> CreateContextKeyAsync(string key, bool defaultValue)
        {
            var ck = new ContextKey(this, key, defaultValue);

            return InvokeScriptAsync("createContext", ck).ContinueWith((noop) =>
            {
                return ck;
            }).AsAsyncOperation();
        }

        public IModel GetModel()
        {
            return _model;
        }

        public IAsyncOperation<IEnumerable<Marker>?> GetModelMarkersAsync() // TODO: Filter (string? owner, Uri? resource, int? take)
        {
            return SendScriptAsync<IEnumerable<Marker>?>("monaco.editor.getModelMarkers();").AsAsyncOperation();
        }

        public IAsyncAction SetModelMarkersAsync(string owner, [ReadOnlyArray] IMarkerData[] markers)
        {
            return SendScriptAsync("monaco.editor.setModelMarkers(model, " + JsonConvert.ToString(owner) + ", " + JsonConvert.SerializeObject(markers) + ");").AsAsyncAction();
        }

        public IAsyncOperation<Position?> GetPositionAsync()
        {
            return SendScriptAsync<Position?>("editor.getPosition();").AsAsyncOperation();
        }

        public IAsyncAction SetPositionAsync(IPosition position)
        {
            return SendScriptAsync("editor.setPosition(" + JsonConvert.SerializeObject(position) + ");").AsAsyncAction();
        }

        /// <summary>
        /// https://microsoft.github.io/monaco-editor/api/interfaces/monaco.editor.icommoncodeeditor.html#deltadecorations
        /// 
        /// Using <see cref="Decorations"/> Property to manipulate decorations instead of calling this directly.
        /// </summary>
        /// <param name="newDecorations"></param>
        /// <returns></returns>
        private IAsyncAction DeltaDecorationsHelperAsync([ReadOnlyArray] ModelDeltaDecoration[] newDecorations)
        {
            var newDecorationsAdjust = newDecorations ?? Array.Empty<ModelDeltaDecoration>();

            // Update Styles
            return InvokeScriptAsync("updateStyle", CssStyleBroker.GetInstance(this).GetStyles()).ContinueWith((noop) =>
            {
                // Send Command to Modify Decorations
                // IMPORTANT: Need to cast to object here as we want this to be a single array object passed as a parameter, not a list of parameters to expand.
                return InvokeScriptAsync("updateDecorations", (object)newDecorationsAdjust);
            }).AsAsyncAction();
        }
    }
}
