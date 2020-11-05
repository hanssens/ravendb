using System;
using System.Threading.Tasks;
using Sparrow.Collections;
using Sparrow.Logging;

namespace Raven.Server.Utils
{
    public class ExceptionAggregator
    {
        private readonly Logger _logger;
        private readonly string _errorMsg;
        private readonly ConcurrentSet<Exception> _list = new ConcurrentSet<Exception>();

        public ExceptionAggregator(string errorMsg)
            : this(null, errorMsg)
        {
        }

        public ExceptionAggregator(Logger logger, string errorMsg)
        {
            _logger = logger;
            _errorMsg = errorMsg;
        }

        public void Execute(IDisposable d)
        {
            try
            {
                d?.Dispose();
            }
            catch (Exception e)
            {
                _list.Add(e);
            }
        }

        public void Execute(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                _list.Add(e);
            }
        }

        public async Task ExecuteAsync(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                _list.Add(e);
            }
        }

        public void ThrowIfNeeded()
        {
            if (_list.Count == 0)
                return;

            var aggregateException = new AggregateException(_errorMsg, _list);

            if (_logger != null && _logger.IsInfoEnabled)
                _logger.Info(_errorMsg, aggregateException);

            throw aggregateException;
        }
    }
}
