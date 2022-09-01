using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace common.server
{
    public interface IPlugin
    {
        void LoadBefore(ServiceCollection services, Assembly[] assemblys);
        void LoadAfter(ServiceProvider services, Assembly[] assemblys);
    }

    public static class PluginLoader
    {

    }
}
