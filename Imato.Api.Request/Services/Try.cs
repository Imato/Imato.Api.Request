namespace Imato.Api.Request
{
    public static class Try
    {
        /// <summary>
        /// Add to pipeline execution options
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="options"></param>
        public static Execution<T> Setup<T>(TryOptions options)
        {
            return new Execution<T>
            {
                Options = options
            };
        }

        /// <summary>
        /// Add to pipeline execution options
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="options"></param>
        public static Execution Setup(TryOptions options)
        {
            return new Execution
            {
                Options = options
            };
        }

        /// <summary>
        /// Add to pipeline execution options
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="options"></param>
        public static Execution<T> Setup<T>(this Execution<T> execution, TryOptions options)
        {
            execution.Options = options;
            return execution;
        }

        /// <summary>
        /// Add to pipeline execution options
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="options"></param>
        public static Execution Setup(this Execution execution, TryOptions options)
        {
            execution.Options = options;
            return execution;
        }

        /// <summary>
        /// Add to pipeline default returned value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="defaultValue">Default value</param>
        /// <returns></returns>
        public static Execution<T> Default<T>(this Execution<T> execution, T defaultValue)
        {
            execution.Default = defaultValue;
            return execution;
        }

        /// <summary>
        /// Add executed function to pipeline
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        public static Execution Function(Func<CancellationToken, Task> func)
        {
            var execution = new Execution();
            execution.AddFunction(func);
            return execution;
        }

        /// <summary>
        /// Add executed function to pipeline
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        public static Execution Function(Func<Task> func)
        {
            var execution = new Execution();
            execution.AddFunction(func);
            return execution;
        }

        /// <summary>
        /// Add executed function to pipeline
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        public static Execution Function(this Execution execution,
            Func<Task> func)
        {
            execution.AddFunction(func);
            return execution;
        }

        /// <summary>
        /// Add executed function to pipeline
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        public static Execution Function(this Execution execution,
            Func<CancellationToken, Task> func)
        {
            execution.AddFunction(func);
            return execution;
        }

        /// <summary>
        /// Add executed function to pipeline
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        public static Execution<T> Function<T>(Func<CancellationToken, Task<T>> func)
        {
            var execution = new Execution<T>();
            execution.AddFunction(func);
            return execution;
        }

        /// <summary>
        /// Add executed function to pipeline
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        public static Execution<T> Function<T>(Func<Task<T>> func)
        {
            var execution = new Execution<T>();
            execution.AddFunction(func);
            return execution;
        }

        /// <summary>
        /// Add executed function to pipeline
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        public static Execution<T> Function<T>(this Execution<T> execution,
            Func<CancellationToken, Task<T>> func)
        {
            execution.AddFunction(func);
            return execution;
        }

        /// <summary>
        /// Add executed function to pipeline
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        public static Execution<T> Function<T>(this Execution<T> execution,
            Func<Task<T>> func)
        {
            execution.AddFunction(func);
            return execution;
        }

        /// <summary>
        /// Add Error handler(s) to pipeline
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        public static Execution<T> OnError<T>(Action<Exception> handler)
        {
            var execution = new Execution<T>();
            execution.AddOnError(handler);
            return execution;
        }

        /// <summary>
        /// Add Error handler(s) to pipeline
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        public static Execution OnError(Action<Exception> handler)
        {
            var execution = new Execution();
            execution.AddOnError(handler);
            return execution;
        }

        /// <summary>
        /// Add Error handler(s) to pipeline
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        public static Execution<T> OnError<T>(this Execution<T> execution,
            Action<Exception> handler)
        {
            execution.AddOnError(handler);
            return execution;
        }

        /// <summary>
        /// Add Error handler(s) to pipeline
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        public static Execution OnError(this Execution execution,
            Action<Exception> handler)
        {
            execution.AddOnError(handler);
            return execution;
        }
    }
}