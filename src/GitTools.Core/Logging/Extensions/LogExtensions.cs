namespace GitTools
{
    using System;
    using System.Runtime.InteropServices;
    using Logging;

    internal static class LogExtensions
    {
        /// <summary>
        /// Writes the specified message as error message and then throws the specified exception.
        /// <para/>
        /// The specified exception must have a constructor that accepts a single string as message.
        /// </summary>
        /// <typeparam name="TException">The type of the exception.</typeparam>
        /// <param name="log">The log.</param>
        /// <param name="messageFormat">The message format.</param>
        /// <param name="args">The args.</param>
        /// <example>
        ///   <code>
        /// This example logs an error and immediately throws the exception:<para/>
        ///   <![CDATA[
        /// Log.ErrorAndThrowException<NotSupportedException>("This action is not supported");
        /// ]]>
        ///   </code>
        ///   </example>
        /// <exception cref="ArgumentNullException">The <paramref name="log"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">The <typeparamref name="TException"/> does not have a constructor accepting a string.</exception>
        public static TException ErrorAndCreateException<TException>(this ILog log, string messageFormat, params object[] args)
            where TException : Exception
        {
            var message = messageFormat ?? string.Empty;
            if (args != null && args.Length > 0)
            {
                message = string.Format(message, args);
            }

            if (log != null)
            {
                log.Error(message);
            }

            Exception exception;

            try
            {
                exception = (Exception)Activator.CreateInstance(typeof(TException), message);
            }
#if !NETFX_CORE && !PCL
            catch (MissingMethodException)
#else
            catch (Exception)
#endif
            {
                var error = string.Format("Exception type '{0}' does not have a constructor accepting a string", typeof(TException).Name);

                if (log != null)
                {
                    log.Error(error);
                }

                throw new NotSupportedException(error);
            }

            return (TException)exception;
        }
    }
}