using System;
using System.Reflection;

namespace GitVersion.MSBuildTask
{
    public class TaskProxy<TTask>
       where TTask : IProxiedTask
    {
        private readonly TTask _taskInstance;
        public Func<bool> InvokeProxiedExecute = null;       

        public TaskProxy(IAssemblyProvider assyProvider, TTask taskInstance)
        {
            if (taskInstance == null)
            {
                throw new ArgumentNullException(nameof(taskInstance));
            }

            _taskInstance = taskInstance;
            var taskType = typeof(TTask);
            var taskAssemblyName = taskType.Assembly.GetName().Name;

            // load the same task type form a copy of the assembly - that is why we are a proxy.
            var proxyAssembly = assyProvider.GetAssembly(taskAssemblyName);
            if (proxyAssembly == null)
            {
                throw new Exception("Unable to load assembly: " + taskAssemblyName);
            }

            var typeName = taskType.FullName;
            var type = proxyAssembly.GetType(typeName, throwOnError: true).GetTypeInfo();
            if (type == null)
            {
                throw new Exception("Unable to load type: " + typeName + " from assembly: " + taskAssemblyName);
            }

            // This is the method that can be used to fire `OnProxyExecute` in the proxied task instance.
            var methodName = nameof(IProxiedTask.OnProxyExecute);
            InvokeProxiedExecute = GetInvokeTaskFunc(type, methodName);
        }

        private Func<bool> GetInvokeTaskFunc(TypeInfo type, string name)
        {
            var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new Exception("Unable to find method named: " + name + " on type: " + type.FullName);
            }
            return () =>
            {
                return _taskInstance.OnProxyExecute();
                //var proxyInstance = (TTask)type.Assembly.CreateInstance(type.FullName);
                //proxyInstance.BuildEngine = _taskInstance.BuildEngine;
                //proxyInstance.HostObject = _taskInstance.HostObject;
                //return (bool)method.Invoke(proxyInstance, null);
            };

            //var delegateInvokeMethod = method.CreateDelegate(typeof(Func<bool>), _taskInstance);
            //return (Func<bool>)delegateInvokeMethod;
        }

    }
}
