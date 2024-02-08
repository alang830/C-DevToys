using Microsoft.Extensions.DependencyInjection;
using WebKit;

namespace DevToys.Linux.Components;

internal sealed class BlazorWebView : WebView
{
    private readonly BlazorWebViewBridge _bridge;

    static BlazorWebView()
    {
        WebKit.Module.Initialize();
    }

    internal BlazorWebView(IServiceProvider serviceProvider)
    {
        _bridge
            = new BlazorWebViewBridge(
                this,
                serviceProvider,
                serviceProvider.GetRequiredService<BlazorWebViewOptions>());
    }
}
