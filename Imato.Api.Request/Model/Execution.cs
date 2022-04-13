namespace Imato.Api.Request
{
    public class Execution
    {
        protected Action<Exception> defaultOnError = (Exception ex) => throw ex;
        private List<Func<CancellationToken, Task>> onExecute = new List<Func<CancellationToken, Task>>();
        protected List<Action<Exception>> onError = new List<Action<Exception>>();
        internal TryOptions Options { get; set; } = new TryOptions();

        internal void AddFunction(Func<Task> func)
        {
            onExecute.Add((CancellationToken token) => func());
        }

        internal void AddFunction(Func<CancellationToken, Task> func)
        {
            onExecute.Add(func);
        }

        internal void AddOnError(Action<Exception> handler)
        {
            onError.Add(handler);
        }

        protected async Task OnError(Exception ex)
        {
            if (Options.RetryCount == 1 && Options.ErrorOnFail)
            {
                if (onError.Count > 0)
                {
                    foreach (var error in onError)
                    {
                        error(ex);
                    }
                }
                else
                {
                    defaultOnError(ex);
                }
            }

            Options.RetryCount--;
            if (Options.Delay > 0 && Options.RetryCount > 0)
            {
                await Task.Delay(Options.Delay);
            }
        }

        /// <summary>
        /// Execute added function(s)
        /// </summary>
        /// <exception cref="Exception"></exception>
        public async Task Execute()
        {
            if (onExecute.Count == 0)
            {
                throw new Exception("Add Function(s) to execute first");
            }

            var hasError = false;
            while (Options.RetryCount > 0)
            {
                foreach (var execution in onExecute)
                {
                    try
                    {
                        await execution(Token);
                    }
                    catch (Exception ex)
                    {
                        hasError = true;
                        await OnError(ex);
                    }
                }

                if (!hasError) return;
            }
        }

        /// <summary>
        /// Get new token
        /// </summary>
        public CancellationToken Token
        {
            get
            {
                var source = new CancellationTokenSource();
                source.CancelAfter(Options.Timeout);
                return source.Token;
            }
        }
    }

    public class Execution<T> : Execution
    {
        private List<Func<CancellationToken, Task<T>>> onExecute = new List<Func<CancellationToken, Task<T>>>();
        public T? Default { get; set; } = default;

        internal void AddFunction(Func<Task<T>> func)
        {
            onExecute.Add((CancellationToken token) => func());
        }

        internal void AddFunction(Func<CancellationToken, Task<T>> func)
        {
            onExecute.Add(func);
        }

        /// <summary>
        /// Get result from pipeline function(s)
        /// </summary>
        /// <returns>T Result of added function(s)</returns>
        /// <exception cref="Exception">Then has not result</exception>
        public async Task<T> GetResultNotEmpty()
        {
            var resutl = await GetResult();
            if (resutl != null) return resutl;
            throw new Exception("Empty result");
        }

        /// <summary>
        /// Get result from pipeline function(s)
        /// </summary>
        /// <returns>T Result of added function(s)</returns>
        /// <exception cref="Exception"></exception>
        public async Task<T?> GetResult()
        {
            if (onExecute.Count == 0)
            {
                throw new Exception("Add Function(s) to execute first");
            }

            foreach (var execution in onExecute)
            {
                while (Options.RetryCount > 0)
                {
                    try
                    {
                        return await execution(Token);
                    }
                    catch (Exception ex)
                    {
                        await OnError(ex);
                    }
                }
            }

            return Default;
        }
    }
}