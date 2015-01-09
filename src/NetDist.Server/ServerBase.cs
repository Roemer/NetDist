using System;
using NetDist.Logging;

namespace NetDist.Server
{
    /// <summary>
    /// Abstract class for server implementations
    /// </summary>
    /// <typeparam name="TSer">Type of the serialized values</typeparam>
    public abstract class ServerBase<TSer>
    {
        /// <summary>
        /// Logger object
        /// </summary>
        public LoggerBase Logger { get; set; }

        /// <summary>
        /// Abstract method to start the server
        /// </summary>
        protected abstract bool StartServer();

        /// <summary>
        /// Abstract method to stop the server
        /// </summary>
        protected abstract void StopServer();


        // TODO: This is in concept phase
        /*public abstract TSer Serialize(object obj);

        public virtual T Deserialize<T>(TSer serializedObject)
        {
            return (T)Deserialize(serializedObject, typeof(T));
        }

        public virtual object Deserialize(TSer serializedObject, Type type)
        {
            var method = GetType().GetMethod("Deserialize").MakeGenericMethod(new[] { type });
            return method.Invoke(this, new object[] { serializedObject });
        }

        public T Deserialize<T>(TSer serializedObject, T example)
        {
            return Deserialize<T>(serializedObject);
        }*/
    }
}
